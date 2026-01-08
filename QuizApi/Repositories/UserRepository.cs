using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuizApi.Constants;
using QuizApi.DTOs.Identity;
using QuizApi.DTOs.Request;
using QuizApi.Extensions;
using QuizApi.Exceptions;
using QuizApi.Helpers;
using QuizApi.Models;
using QuizApi.Models.Identity;
using QuizApi.Responses;
using QuizApi.DTOs.Quiz;
using QuizApi.Models.QuizHistory;
using QuizApi.DTOs.QuizHistory;

namespace QuizApi.Repositories
{
    public class UserRepository
    {
        private readonly QuizAppDBContext dBContext;
        private readonly IMapper mapper;
        private readonly ActionModelHelper actionModelHelper;
        private readonly string userId = "";
        // private readonly string tableName = "User";
        public UserRepository(
            QuizAppDBContext dBContext,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor
        )
        {
            this.dBContext = dBContext;
            this.mapper = mapper;
            actionModelHelper = new ActionModelHelper();

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        public async Task<SearchResponse> SearchDatasAsync(SearchRequestDto searchRequest)
        {
            IQueryable<UserModel> listUserQuery = dBContext.User
                .Where(x => x.RecordStatus == RecordStatusConstant.Active && x.UserId != userId)
                .AsQueryable();

            #region Query
            if (!string.IsNullOrWhiteSpace(searchRequest.Search))
            {
                listUserQuery = listUserQuery.Where(x => EF.Functions.ILike(x.Name, $"%{searchRequest.Search}%"));
            }
            #endregion

            #region Ordering
            string orderBy = searchRequest.OrderBy;
            string orderDir = searchRequest.OrderDir;

            if (orderBy.Equals("createdTime"))
            {
                if (orderDir.Equals("asc"))
                {
                    listUserQuery = listUserQuery.OrderBy(x => x.CreatedTime).AsQueryable();
                }
                else if (orderDir.Equals("desc"))
                {
                    listUserQuery = listUserQuery.OrderByDescending(x => x.CreatedTime).AsQueryable();
                }
            }
            #endregion

            var response = new SearchResponse();
            response.TotalItems = await listUserQuery.CountAsync();
            response.CurrentPage = searchRequest.CurrentPage;
            response.PageSize = searchRequest.PageSize;

            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var take = searchRequest.PageSize;
            var listUser = await listUserQuery.Skip(skip).Take(take).ToListAsync();

            response.Items = mapper.Map<List<UserDto>>(listUser);

            return response;
        }

        public async Task<UserDto> GetDataByIdAsync(string id)
        {
            UserModel? user = await GetActiveUserByIdAsync(id);

            if (user is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            UserDto categoryDto = mapper.Map<UserDto>(user);

            return categoryDto;
        }

        public async Task UpdateDataAsync(string id, UserDto userDto)
        {
            UserModel? user = await dBContext.User
                .Where(x => x.UserId.Equals(id) && x.RecordStatus == RecordStatusConstant.Active)
                .FirstOrDefaultAsync();;

            if (user is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            user.Name = userDto.Name;
            user.Email = userDto.Email;
            user.Description = userDto.Description ?? "";
            user.ProfileImage = userDto.ProfileImage;
            user.RoleId = userDto.RoleId;

            actionModelHelper.AssignUpdateModel(user, userId);            

            dBContext.Update(user);
            await dBContext.SaveChangesAsync();
        }

        public async Task DeleteDataAsync(string id)
        {
            UserModel? user = await GetActiveUserByIdAsync(id);

            if (user is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            actionModelHelper.AssignDeleteModel(user, userId);

            dBContext.Update(user);
            await dBContext.SaveChangesAsync();
        }

        public async Task<SimpleUserDto> GetSimpleUserDtoAsync(string id)
        {
            UserModel? user = await GetActiveUserByIdAsync(id);

            if (user is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            SimpleUserDto simpleUser = mapper.Map<SimpleUserDto>(user);

            return simpleUser;
        }

        public async Task<SearchResponse> GetQuizzesByUserIdAsync(string id, SearchRequestDto searchRequest)
        {
            IQueryable<QuizDto> listQuizzesQuery = dBContext.Quiz
                .Where(x => x.RecordStatus == RecordStatusConstant.Active && x.UserId == id)
                .Select(x => new QuizDto
                {
                    QuizId = x.QuizId,
                    Title = x.Title,
                    Description = x.Description,
                    ImageUrl = x.ImageUrl,
                    CategoryId = x.CategoryId,
                    Category = mapper.Map<CategoryDto>(x.Category),
                    Time = x.Time,
                    UserId = x.UserId,
                    CreatedBy = x.CreatedBy,
                    CreatedTime = x.CreatedTime,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedTime = x.ModifiedTime,
                    Version = x.Version,
                    RecordStatus = x.RecordStatus,
                    QuestionCount = x.Questions.Count(q => q.RecordStatus == RecordStatusConstant.Active),
                    HistoriesCount = x.Histories.Count(),
                    // check if the current user has taken the quiz
                    IsTakenByUser = x.Histories.Any(y => y.UserId == userId)
                });

            // sorting
            listQuizzesQuery = listQuizzesQuery.OrderByDescending(x => x.CreatedTime);

            var response = new SearchResponse();
            response.TotalItems = await listQuizzesQuery.CountAsync();
            response.CurrentPage = searchRequest.CurrentPage;
            response.PageSize = searchRequest.PageSize;

            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var take = searchRequest.PageSize;
            var listQuiz = await listQuizzesQuery.Skip(skip).Take(take).ToListAsync();

            response.Items = listQuiz;

            return response;
        }

        public async Task<SearchResponse> GetHistoriesByUserIdAsync(string id, SearchRequestDto searchRequest)
        {
            IQueryable<QuizHistoryModel> listQuizHistoriesQuery = dBContext.QuizHistory
                .Where(x => x.RecordStatus == RecordStatusConstant.Active && x.UserId == id)
                .Include(x => x.User)
                .Include(x => x.Quiz)
                .AsQueryable();

            // sorting
            listQuizHistoriesQuery = listQuizHistoriesQuery.OrderByDescending(x => x.CreatedTime);

            var response = new SearchResponse();
            response.TotalItems = await listQuizHistoriesQuery.CountAsync();
            response.CurrentPage = searchRequest.CurrentPage;
            response.PageSize = searchRequest.PageSize;

            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var take = searchRequest.PageSize;
            var listQuizHistories = await listQuizHistoriesQuery.Skip(skip).Take(take).ToListAsync();

            response.Items = mapper.Map<List<QuizHistoryDto>>(listQuizHistories);

            return response;
        }

        public async Task<SearchResponse> GetSelfQuizzesAsync(SearchRequestDto searchRequest)
        {
            IQueryable<QuizDto> listQuizzesQuery = dBContext.Quiz
                .Where(x => x.RecordStatus == RecordStatusConstant.Active && x.UserId == userId)
                .Select(x => new QuizDto
                {
                    QuizId = x.QuizId,
                    Title = x.Title,
                    Description = x.Description,
                    ImageUrl = x.ImageUrl,
                    CategoryId = x.CategoryId,
                    Category = mapper.Map<CategoryDto>(x.Category),
                    Time = x.Time,
                    UserId = x.UserId,
                    CreatedBy = x.CreatedBy,
                    CreatedTime = x.CreatedTime,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedTime = x.ModifiedTime,
                    Version = x.Version,
                    RecordStatus = x.RecordStatus,
                    QuestionCount = x.Questions.Count(q => q.RecordStatus == RecordStatusConstant.Active),
                    HistoriesCount = x.Histories.Count()
                });

            // sorting
            listQuizzesQuery = listQuizzesQuery.OrderByDescending(x => x.CreatedTime);

            var response = new SearchResponse();
            response.TotalItems = await listQuizzesQuery.CountAsync();
            response.CurrentPage = searchRequest.CurrentPage;
            response.PageSize = searchRequest.PageSize;

            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var take = searchRequest.PageSize;
            var listQuiz = await listQuizzesQuery.Skip(skip).Take(take).ToListAsync();

            response.Items = listQuiz;

            return response;
        }

        public async Task<SearchResponse> GetSelfHistoriesAsync(SearchRequestDto searchRequest)
        {
            IQueryable<QuizHistoryModel> listQuizHistoriesQuery = dBContext.QuizHistory
                .Where(x => x.RecordStatus == RecordStatusConstant.Active && x.UserId == userId)
                .Include(x => x.User)
                .Include(x => x.Quiz)
                .AsQueryable();

            // sorting
            listQuizHistoriesQuery = listQuizHistoriesQuery.OrderByDescending(x => x.CreatedTime);

            var response = new SearchResponse();
            response.TotalItems = await listQuizHistoriesQuery.CountAsync();
            response.CurrentPage = searchRequest.CurrentPage;
            response.PageSize = searchRequest.PageSize;

            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var take = searchRequest.PageSize;
            var listQuizHistories = await listQuizHistoriesQuery.Skip(skip).Take(take).ToListAsync();

            response.Items = mapper.Map<List<QuizHistoryDto>>(listQuizHistories);

            return response;
        }
        
        private async Task<UserModel?> GetActiveUserByIdAsync(string id)
        {
            return await dBContext.User
                .Where(x => x.UserId.Equals(id) && x.RecordStatus == RecordStatusConstant.Active)
                .Include(x => x.Role)
                .FirstOrDefaultAsync();
        }
    }
}