using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuizApi.Constants;
using QuizApi.DTOs.Identity;
using QuizApi.DTOs.Request;
using QuizApi.Exceptions;
using QuizApi.Models;
using QuizApi.Models.Identity;
using QuizApi.Responses;

namespace QuizApi.Repositories
{
    public class UserRepository
    {
        private readonly QuizAppDBContext dBContext;
        private readonly IMapper mapper;
        public UserRepository(QuizAppDBContext dBContext, IMapper mapper)
        {
            this.dBContext = dBContext;
            this.mapper = mapper;
        }

        public async Task<SearchResponse> SearchDatasAsync(SearchRequestDto searchRequest)
        {
            IQueryable<UserModel> listUserQuery = dBContext.User
                .Where(x => x.RecordStatus == RecordStatusConstant.Active)
                .AsQueryable();

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
            UserModel? user = await GetActiveUserByIdAsync(id);

            if (user is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            user.Name = userDto.Name;
            user.Email = userDto.Email;
            user.Description = userDto.Description ?? "";
            user.ProfileImage = userDto.ProfileImage;

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

            user.RecordStatus = RecordStatusConstant.Deleted;

            dBContext.Update(user);
            await dBContext.SaveChangesAsync();
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