using AutoMapper;
using QuizApi.Constants;
using QuizApi.DTOs.Identity;
using QuizApi.DTOs.Request;
using QuizApi.Extensions;
using QuizApi.Exceptions;
using QuizApi.Helpers;
using QuizApi.Models;
using QuizApi.Models.Identity;
using QuizApi.Responses;
using QuizApi.Models.QuizHistory;
using QuizApi.DTOs.QuizHistory;
using Google.Cloud.Firestore;
using QuizApi.Models.Quiz;

namespace QuizApi.Repositories
{
    public class UserRepository
    {
        private readonly FirestoreDb firestoreDb;
        private readonly string CollectionName = "user";
        private readonly IMapper mapper;
        private readonly ActionModelHelper actionModelHelper;
        private readonly string userId = "";
        private readonly RoleRepository roleRepository;
        // private readonly string tableName = "User";
        public UserRepository(
            [FromKeyedServices("quiz-db")] FirestoreDb firestoreDb,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            RoleRepository roleRepository
        )
        {
            this.firestoreDb = firestoreDb;
            this.mapper = mapper;
            this.roleRepository = roleRepository;
            actionModelHelper = new ActionModelHelper();

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        public async Task<SearchResponse> SearchDatasAsync(SearchRequestDto searchRequest)
        {
            // 1. Reference the collection
            CollectionReference usersRef = firestoreDb.Collection(CollectionName);

            // 2. Build the Query (Filtering)
            Query query = usersRef.WhereEqualTo("RecordStatus", RecordStatusConstant.Active);

            // 3. Ordering
            string orderByField = searchRequest.OrderBy == "createdTime" ? "CreatedTime" : "Name";
            if (searchRequest.OrderDir == "desc")
                query = query.OrderByDescending(orderByField);
            else
                query = query.OrderBy(orderByField);

            // 4. Execution & Pagination
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            
            var allDocs = snapshot.Documents
                .Select(d => d.ConvertTo<UserModel>())
                .Where(u => u.UserId != userId);

            if (!string.IsNullOrWhiteSpace(searchRequest.Search))
            {
                allDocs = allDocs.Where(u => u.Name.Contains(searchRequest.Search, StringComparison.OrdinalIgnoreCase));
            }

            var response = new SearchResponse
            {
                TotalItems = allDocs.Count(),
                CurrentPage = searchRequest.CurrentPage,
                PageSize = searchRequest.PageSize
            };

            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var pagedList = allDocs.Skip(skip).Take(searchRequest.PageSize).ToList();

            response.Items = mapper.Map<List<UserDto>>(pagedList);

            return response;
        }

        public async Task<UserDto> GetDataByIdAsync(string id)
        {
            UserModel? user = await GetActiveUserByIdAsync(id);

            if (user is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            UserDto userDto = mapper.Map<UserDto>(user);

            if (userDto.RoleId != null)
            {
                userDto.Role = await roleRepository.GetDataByIdAsync(userDto.RoleId);
            }

            return userDto;
        }

        public async Task UpdateDataAsync(string id, UserDto userDto)
        {
            Query query = firestoreDb.Collection("user")
                .WhereEqualTo("UserId", id)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            DocumentSnapshot doc = snapshot.Documents[0];
            UserModel user = doc.ConvertTo<UserModel>();
            WriteBatch batch = firestoreDb.StartBatch();

            user.Name = userDto.Name;
            user.Email = userDto.Email;
            user.Description = userDto.Description ?? "";
            user.ProfileImage = userDto.ProfileImage;
            user.RoleId = userDto.RoleId;

            actionModelHelper.AssignUpdateModel(user, userId);            

            batch.Set(doc.Reference, user);
            await batch.CommitAsync();
        }

        public async Task DeleteDataAsync(string id)
        {
            Query query = firestoreDb.Collection("user")
                .WhereEqualTo("UserId", id)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            DocumentSnapshot doc = snapshot.Documents[0];
            UserModel user = doc.ConvertTo<UserModel>();

            WriteBatch batch = firestoreDb.StartBatch();

            actionModelHelper.AssignDeleteModel(user, userId);

            batch.Set(doc.Reference, user);
            await batch.CommitAsync();
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

        // public async Task<SearchResponse> GetQuizzesByUserIdAsync(string id, SearchRequestDto searchRequest)
        // {
        //     IQueryable<QuizDto> listQuizzesQuery = dBContext.Quiz
        //         .Where(x => x.RecordStatus == RecordStatusConstant.Active && x.UserId == id)
        //         .Select(x => new QuizDto
        //         {
        //             QuizId = x.QuizId,
        //             Title = x.Title,
        //             Description = x.Description,
        //             ImageUrl = x.ImageUrl,
        //             CategoryId = x.CategoryId,
        //             Category = mapper.Map<CategoryDto>(x.Category),
        //             Time = x.Time,
        //             UserId = x.UserId,
        //             CreatedBy = x.CreatedBy,
        //             CreatedTime = x.CreatedTime,
        //             ModifiedBy = x.ModifiedBy,
        //             ModifiedTime = x.ModifiedTime,
        //             Version = x.Version,
        //             RecordStatus = x.RecordStatus,
        //             QuestionCount = x.Questions.Count(q => q.RecordStatus == RecordStatusConstant.Active),
        //             HistoriesCount = x.Histories.Count(),
        //             // check if the current user has taken the quiz
        //             IsTakenByUser = x.Histories.Any(y => y.UserId == userId)
        //         });

        //     // sorting
        //     listQuizzesQuery = listQuizzesQuery.OrderByDescending(x => x.CreatedTime);

        //     var response = new SearchResponse();
        //     response.TotalItems = await listQuizzesQuery.CountAsync();
        //     response.CurrentPage = searchRequest.CurrentPage;
        //     response.PageSize = searchRequest.PageSize;

        //     var skip = searchRequest.PageSize * searchRequest.CurrentPage;
        //     var take = searchRequest.PageSize;
        //     var listQuiz = await listQuizzesQuery.Skip(skip).Take(take).ToListAsync();

        //     response.Items = listQuiz;

        //     return response;
        // }

        public async Task<SearchResponse> GetHistoriesByUserIdAsync(string id, SearchRequestDto searchRequest)
        {
            // 1. Setup Collection Reference
            CollectionReference collection = firestoreDb.Collection("quizhistory");

            // 2. Build the Query
            // Filter by Active status and the specific User ID
            Query query = collection
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .WhereEqualTo("UserId", id)
                .OrderByDescending("CreatedTime");

            // 3. Get Total Count for metadata
            AggregateQuery countQuery = query.Count();
            AggregateQuerySnapshot countSnapshot = await countQuery.GetSnapshotAsync();
            int totalItems = (int)(countSnapshot.Count ?? 0);

            // 4. Apply Pagination
            int skip = searchRequest.PageSize * searchRequest.CurrentPage;
            int take = searchRequest.PageSize;

            Query pagedQuery = query.Offset(skip).Limit(take);
            QuerySnapshot querySnapshot = await pagedQuery.GetSnapshotAsync();

            // 5. Convert Documents to Models
            var listQuizHistories = querySnapshot.Documents
                .Select(doc => doc.ConvertTo<QuizHistoryModel>())
                .ToList();

            foreach (var history in listQuizHistories)
            {
                if (history.UserId != null)
                {
                    Query userQuery = firestoreDb.Collection("user")
                        .WhereEqualTo("UserId", history.UserId)
                        .Limit(1);

                    QuerySnapshot snapshot = await userQuery.GetSnapshotAsync();

                    if (snapshot.Documents.Count > 0)
                    {
                        UserModel user = snapshot.Documents[0].ConvertTo<UserModel>();
                        history.User = user;
                    }
                }

                if (history.QuizId != null)
                {
                    Query quizQuery = firestoreDb.Collection("quiz")
                        .WhereEqualTo("QuizId", history.QuizId)
                        .Limit(1);

                    QuerySnapshot snapshot = await quizQuery.GetSnapshotAsync();

                    if (snapshot.Documents.Count > 0)
                    {
                        QuizModel quiz = snapshot.Documents[0].ConvertTo<QuizModel>();
                        history.Quiz = quiz;
                    }
                }
            }

            return new SearchResponse
            {
                TotalItems = totalItems,
                CurrentPage = searchRequest.CurrentPage,
                PageSize = searchRequest.PageSize,
                Items = mapper.Map<List<QuizHistoryDto>>(listQuizHistories)
            };
        }

        // public async Task<SearchResponse> GetSelfQuizzesAsync(SearchRequestDto searchRequest)
        // {
        //     IQueryable<QuizDto> listQuizzesQuery = dBContext.Quiz
        //         .Where(x => x.RecordStatus == RecordStatusConstant.Active && x.UserId == userId)
        //         .Select(x => new QuizDto
        //         {
        //             QuizId = x.QuizId,
        //             Title = x.Title,
        //             Description = x.Description,
        //             ImageUrl = x.ImageUrl,
        //             CategoryId = x.CategoryId,
        //             Category = mapper.Map<CategoryDto>(x.Category),
        //             Time = x.Time,
        //             UserId = x.UserId,
        //             CreatedBy = x.CreatedBy,
        //             CreatedTime = x.CreatedTime,
        //             ModifiedBy = x.ModifiedBy,
        //             ModifiedTime = x.ModifiedTime,
        //             Version = x.Version,
        //             RecordStatus = x.RecordStatus,
        //             QuestionCount = x.Questions.Count(q => q.RecordStatus == RecordStatusConstant.Active),
        //             HistoriesCount = x.Histories.Count()
        //         });

        //     // sorting
        //     listQuizzesQuery = listQuizzesQuery.OrderByDescending(x => x.CreatedTime);

        //     var response = new SearchResponse();
        //     response.TotalItems = await listQuizzesQuery.CountAsync();
        //     response.CurrentPage = searchRequest.CurrentPage;
        //     response.PageSize = searchRequest.PageSize;

        //     var skip = searchRequest.PageSize * searchRequest.CurrentPage;
        //     var take = searchRequest.PageSize;
        //     var listQuiz = await listQuizzesQuery.Skip(skip).Take(take).ToListAsync();

        //     response.Items = listQuiz;

        //     return response;
        // }

        public async Task<SearchResponse> GetSelfHistoriesAsync(SearchRequestDto searchRequest)
        {
            // 1. Setup Collection Reference
            CollectionReference collection = firestoreDb.Collection("quizhistory");

            // 2. Build the Query
            // Filter by Active status and the specific User ID
            Query query = collection
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .WhereEqualTo("UserId", userId)
                .OrderByDescending("CreatedTime");

            // 3. Get Total Count for metadata
            AggregateQuery countQuery = query.Count();
            AggregateQuerySnapshot countSnapshot = await countQuery.GetSnapshotAsync();
            int totalItems = (int)(countSnapshot.Count ?? 0);

            // 4. Apply Pagination
            int skip = searchRequest.PageSize * searchRequest.CurrentPage;
            int take = searchRequest.PageSize;

            Query pagedQuery = query.Offset(skip).Limit(take);
            QuerySnapshot querySnapshot = await pagedQuery.GetSnapshotAsync();

            // 5. Convert Documents to Models
            var listQuizHistories = querySnapshot.Documents
                .Select(doc => doc.ConvertTo<QuizHistoryModel>())
                .ToList();

            foreach (var history in listQuizHistories)
            {
                if (history.UserId != null)
                {
                    Query userQuery = firestoreDb.Collection("user")
                        .WhereEqualTo("UserId", history.UserId)
                        .Limit(1);

                    QuerySnapshot snapshot = await userQuery.GetSnapshotAsync();

                    if (snapshot.Documents.Count > 0)
                    {
                        UserModel user = snapshot.Documents[0].ConvertTo<UserModel>();
                        history.User = user;
                    }
                }

                if (history.QuizId != null)
                {
                    Query quizQuery = firestoreDb.Collection("quiz")
                        .WhereEqualTo("QuizId", history.QuizId)
                        .Limit(1);

                    QuerySnapshot snapshot = await quizQuery.GetSnapshotAsync();

                    if (snapshot.Documents.Count > 0)
                    {
                        QuizModel quiz = snapshot.Documents[0].ConvertTo<QuizModel>();
                        history.Quiz = quiz;
                    }
                }
            }

            return new SearchResponse
            {
                TotalItems = totalItems,
                CurrentPage = searchRequest.CurrentPage,
                PageSize = searchRequest.PageSize,
                Items = mapper.Map<List<QuizHistoryDto>>(listQuizHistories)
            };
        }

        public async Task<int> GetSelfQuizCountAsync()
        {
            int quizCount = await GetCollectionCount("quiz", "UserId", userId);

            return quizCount;
        }

        public async Task<int> GetSelfHistoryCountAsync()
        {
            int historyCount = await GetCollectionCount("quizhistory", "UserId", userId);

            return historyCount;
        }
        
        private async Task<UserModel?> GetActiveUserByIdAsync(string id)
        {
            Query query = firestoreDb.Collection("user")
                .WhereEqualTo("UserId", id)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                return snapshot.Documents[0].ConvertTo<UserModel>();
            }

            return null;
        }

        private async Task<int> GetCollectionCount(string collection, string filterField, string filterValue)
        {
            Query query = firestoreDb.Collection(collection)
                .WhereEqualTo(filterField, filterValue)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active);

            AggregateQuerySnapshot snapshot = await query.Count().GetSnapshotAsync();
            return (int)(snapshot.Count ?? 0);
        }
    }
}