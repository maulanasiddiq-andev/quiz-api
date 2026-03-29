using AutoMapper;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;
using QuizApi.Constants;
using QuizApi.DTOs.Identity;
using QuizApi.DTOs.Request;
using QuizApi.Exceptions;
using QuizApi.Extensions;
using QuizApi.Helpers;
using QuizApi.Models;
using QuizApi.Models.Identity;
using QuizApi.Responses;
using QuizApi.Services;

namespace QuizApi.Repositories
{
    public class RoleRepository
    {
        private readonly FirestoreDb firestoreDb;
        private readonly IMapper mapper;
        private readonly QuizAppDBContext dBContext;
        private readonly string userId = "";
        private readonly CacheService cacheService;
        private readonly ActionModelHelper actionModelHelper;
        private readonly string tableName = "Role";
        public RoleRepository(
            [FromKeyedServices("quiz-db")] FirestoreDb firestoreDb,
            IMapper mapper,
            QuizAppDBContext dBContext,
            IHttpContextAccessor httpContextAccessor,
            CacheService cacheService
        )
        {
            this.firestoreDb = firestoreDb;
            this.mapper = mapper;
            this.dBContext = dBContext;
            this.cacheService = cacheService;
            actionModelHelper = new ActionModelHelper();

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        // GET search
        public async Task<SearchResponse> SearchDatasAsync(SearchRequestDto searchRequest)
        {
            // 1. Reference the collection
            CollectionReference rolesRef = firestoreDb.Collection("role");

            // 2. Build the Query (Filtering)
            Query query = rolesRef.WhereEqualTo("RecordStatus", RecordStatusConstant.Active);

            // 3. Ordering
            string orderByField = searchRequest.OrderBy == "createdTime" ? "CreatedTime" : "Name";
            if (searchRequest.OrderDir == "desc")
                query = query.OrderByDescending(orderByField);
            else
                query = query.OrderBy(orderByField);

            // 4. Execution & Pagination
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            
            var allDocs = snapshot.Documents
                .Select(d => d.ConvertTo<RoleModel>());

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

            response.Items = mapper.Map<List<RoleDto>>(pagedList);

            return response;
        }

        // GET role by id
        // used for JWT
        public async Task<RoleDto?> GetDataByIdAsync(string id)
        {
            Query query = firestoreDb.Collection("role").WhereEqualTo("RoleId", id).Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                var role = snapshot.Documents[0].ConvertTo<RoleModel>();
                return mapper.Map<RoleDto>(role);
            }

            return null;
        }

        // GET role by id (with-modules)
        // used for updating role modules
        public async Task<RoleWithModuleDto> GetRoleByIdWithModulesAsync(string id)
        {
            RoleModel? role = await GetActiveRoleByIdAsync(id);

            if (role is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            RoleWithModuleDto roleDto = mapper.Map<RoleWithModuleDto>(role);

            var selectModules = await GetSelectedUnselectedRoleModules(id);
            roleDto.RoleModules = selectModules;

            return roleDto;
        }

        // for mapping modules, selected and unselected modules
        public async Task<List<SelectModuleDto>> GetSelectedUnselectedRoleModules(string roleId)
        {
            var modules = ModuleMappingHelper.GetAllModules();

            var roleModules = await GetRoleModulesByRoleId(roleId);

            List<SelectModuleDto> selectModules = new();

            foreach (var module in modules)
            {
                var selectModule = new SelectModuleDto
                {
                    RoleModuleName = module,
                    IsSelected = roleModules.Exists(x => x.RoleModuleName == module)
                };

                selectModules.Add(selectModule);
            }

            return selectModules;
        }
        
        // for getting modules assigned to the role
        public async Task<List<RoleModuleModel>> GetRoleModulesByRoleId(string roleId)
        {
            CollectionReference roleModuleRef = firestoreDb.Collection("rolemodule");

            Query query = roleModuleRef.WhereEqualTo("RoleId", roleId).WhereEqualTo("RecordStatus", RecordStatusConstant.Active);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            List<RoleModuleModel> roleModules = snapshot.Documents
                .Select(doc => doc.ConvertTo<RoleModuleModel>())
                .ToList();

            return roleModules;
        }

        // POST create role
        public async Task CreateDataAsync(RoleDto roleDto)
        {
            RoleModel role = mapper.Map<RoleModel>(roleDto);

            // if the added role is not main
            // check if there is any main role
            if (role.IsMain == false)
            {
                // if there is not any main role
                // throw
                Query query = firestoreDb.Collection("role").WhereEqualTo("IsMain", true).Limit(1);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count == 0)
                {
                    throw new KnownException("Pilih satu role sebagai role default");
                }
            }

            actionModelHelper.AssignCreateModel(role, tableName, userId);

            await firestoreDb.Collection("role").AddAsync(role);
        }

        // PUT update role by id
        public async Task UpdateDataAsync(string roleId, RoleWithModuleDto roleWithModuleDto)
        {
            RoleModel? role = await GetActiveRoleByIdAsync(roleId);

            if (role is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            #region EditRole
            role.Name = roleWithModuleDto.Name;
            role.Description = roleWithModuleDto.Description ?? "";
            role.IsMain = roleWithModuleDto.IsMain;

            // if the updated role is changed to main, change other roles to IsMain = false
            if (role.IsMain)
            {
                List<RoleModel> roles = await dBContext.Role.Where(x => x.RoleId != roleId).ToListAsync();

                foreach (var item in roles)
                {
                    item.IsMain = false;
                }

                dBContext.UpdateRange(roles);
            }
            // if the edited role is false
            // check if there is main role
            else
            {
                // if there is not main role, throw
                Query query = firestoreDb.Collection("role").WhereEqualTo("IsMain", true).Limit(1);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count == 0)
                {
                    throw new KnownException("Pilih satu role sebagai role default");
                }
            }

            actionModelHelper.AssignUpdateModel(role, userId);
            dBContext.Update(role);
            #endregion

            #region EditRoleModule
            var roleModules = await GetRoleModulesByRoleId(roleId);

            // check if the modules is selected or unselected
            foreach (var module in roleWithModuleDto.RoleModules)
            {
                // get the role module from assigned modules for deciding whether creating a new one or deleting an existing one
                var updatedRoleModule = roleModules.FirstOrDefault(x => x.RoleModuleName == module.RoleModuleName);

                // if the module is previously selected and now unselected
                // delete
                if (updatedRoleModule != null && module.IsSelected == false)
                {
                    dBContext.Remove(updatedRoleModule);
                }

                // if the module is previously unselected and no selected
                // create
                if (updatedRoleModule == null && module.IsSelected == true)
                {
                    var newRoleModule = new RoleModuleModel
                    {
                        RoleModuleId = Guid.NewGuid().ToString("N"),
                        RoleId = roleId,
                        CreatedTime = DateTime.UtcNow,
                        ModifiedTime = DateTime.UtcNow,
                        CreatedBy = userId,
                        ModifiedBy = userId,
                        RecordStatus = RecordStatusConstant.Active,
                        RoleModuleName = module.RoleModuleName
                    };

                    await dBContext.AddAsync(newRoleModule);
                }
            }

            List<string> userIds = await dBContext.User
                .Where(x => x.RecordStatus == RecordStatusConstant.Active && x.RoleId == roleId)
                .Select(x => x.UserId)
                .ToListAsync();

            await cacheService.RemoveUserRelatedCache(userIds);
            #endregion

            await dBContext.SaveChangesAsync();
        }

        public async Task DeleteDataAsync(string id)
        {
            Query query = firestoreDb.Collection("role").WhereEqualTo("RoleId", id).Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            DocumentSnapshot doc = snapshot.Documents[0];
            RoleModel role = doc.ConvertTo<RoleModel>();

            if (role.IsMain == true)
            {
                throw new KnownException("Pilih satu role sebagai role default");
            }

            WriteBatch batch = firestoreDb.StartBatch();

            actionModelHelper.AssignDeleteModel(role, userId);

            batch.Set(doc.Reference, role);
            await batch.CommitAsync();
        }

        private async Task<RoleModel?> GetActiveRoleByIdAsync(string id)
        {
            Query query = firestoreDb.Collection("role").WhereEqualTo("RoleId", id).Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                return null;
            }

            return snapshot.Documents[0].ConvertTo<RoleModel>();
        }
    }
}