using AutoMapper;
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
        private readonly IMapper mapper;
        private readonly QuizAppDBContext dBContext;
        private readonly string userId = "";
        private readonly CacheService cacheService;
        public RoleRepository(
            IMapper mapper,
            QuizAppDBContext dBContext,
            IHttpContextAccessor httpContextAccessor,
            CacheService cacheService
        )
        {
            this.mapper = mapper;
            this.dBContext = dBContext;
            this.cacheService = cacheService;

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        // GET search
        public async Task<SearchResponse> SearchDatasAsync(SearchRequestDto searchRequest)
        {
            IQueryable<RoleModel> listRoleQuery = dBContext.Role
                .Where(x => x.RecordStatus.ToLower().Equals(RecordStatusConstant.Active.ToLower()))
                .AsQueryable();

            #region Ordering
            string orderBy = searchRequest.OrderBy;
            string orderDir = searchRequest.OrderDir;

            if (orderBy.Equals("createdTime"))
            {
                if (orderDir.Equals("asc"))
                {
                    listRoleQuery = listRoleQuery.OrderBy(x => x.CreatedTime).AsQueryable();
                }
                else if (orderDir.Equals("desc"))
                {
                    listRoleQuery = listRoleQuery.OrderByDescending(x => x.CreatedTime).AsQueryable();
                }
            }
            #endregion

            var response = new SearchResponse();
            response.TotalItems = await listRoleQuery.CountAsync();
            response.CurrentPage = searchRequest.CurrentPage;
            response.PageSize = searchRequest.PageSize;

            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var take = searchRequest.PageSize;
            var listRole = await listRoleQuery.Skip(skip).Take(take).ToListAsync();

            response.Items = mapper.Map<List<RoleDto>>(listRole);

            return response;
        }

        // GET role by id
        public async Task<RoleDto> GetDataByIdAsync(string id)
        {
            RoleModel? role = await GetActiveRoleByIdAsync(id);

            if (role is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            RoleDto roleDto = mapper.Map<RoleDto>(role);

            return roleDto;
        }

        // GET role by id (with-modules)
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
            List<RoleModuleModel> roleModules = await dBContext.RoleModule
                .Where(x => x.RoleId.Equals(roleId) && x.RecordStatus == RecordStatusConstant.Active)
                .ToListAsync();

            return roleModules;
        }

        // POST create role
        public async Task CreateDataAsync(RoleDto roleDto)
        {
            RoleModel role = mapper.Map<RoleModel>(roleDto);

            role.RoleId = Guid.NewGuid().ToString("N");
            role.CreatedTime = DateTime.UtcNow;
            role.ModifiedTime = DateTime.UtcNow;
            role.CreatedBy = userId;
            role.ModifiedBy = userId;
            role.RecordStatus = RecordStatusConstant.Active;

            await dBContext.AddAsync(role);
            await dBContext.SaveChangesAsync();
        }

        // PUT update role by id
        public async Task UpdateDataAsync(string id, RoleDto roleDto)
        {
            RoleModel? role = await GetActiveRoleByIdAsync(id);

            if (role is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            role.Name = roleDto.Name;
            role.Description = roleDto.Description ?? "";
            role.IsMain = roleDto.IsMain;

            // if the updated role is changed to main, change other roles to IsMain = false
            if (role.IsMain)
            {
                List<RoleModel> roles = await dBContext.Role.Where(x => x.RoleId != id).ToListAsync();

                foreach (var item in roles)
                {
                    item.IsMain = false;
                }

                dBContext.UpdateRange(roles);
            }

            dBContext.Update(role);
            await dBContext.SaveChangesAsync();
        }

        // PUT update role by id (update-modules)
        public async Task UpdateRoleModulesByIdAsync(string roleId, RoleWithModuleDto roleWithModuleDto)
        {
            RoleModel? role = await GetActiveRoleByIdAsync(roleId);

            if (role is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

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

            cacheService.RemoveUserRelatedCache(userIds);

            await dBContext.SaveChangesAsync();
        }

        private async Task<RoleModel?> GetActiveRoleByIdAsync(string id)
        {
            RoleModel? role = await dBContext.Role
                .Where(x => x.RoleId.Equals(id) && x.RecordStatus == RecordStatusConstant.Active)
                .FirstOrDefaultAsync();

            return role;
        }
    }
}