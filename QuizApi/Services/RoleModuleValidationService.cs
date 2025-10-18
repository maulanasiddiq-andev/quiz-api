using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuizApi.Constants;
using QuizApi.Exceptions;
using QuizApi.Models;
using QuizApi.Models.Identity;

namespace QuizApi.Services
{
    public class RoleModuleValidationService
    {
        private readonly QuizAppDBContext dbContext;
        private readonly CacheService cacheService;
        private readonly ActivityLogService activityLogService;

        public RoleModuleValidationService(
            QuizAppDBContext context,
            ActivityLogService logService,
            CacheService icacheService
        )
        {
            activityLogService = logService;
            dbContext = context;
            cacheService = icacheService;
        }

        public async Task<bool> IsHideOthers(string userId, string modulName)
        {
            return await IsAllowAccessModuleAsync(userId, modulName);
        }

        public async Task<bool> IsAllowAccessModuleAsync(string userId, string modulName)
        {
            bool isAllow = false;

            try
            {
                // Prefer To Use memory DB First
                // Remove memory DB when RoleModul Created or Modified
                IEnumerable<RoleModuleModel>? roleModuleInCache = cacheService.GetData<IEnumerable<RoleModuleModel>>(MemoryCacheConstant.RoleModuleKey + userId);

                if (roleModuleInCache != null)
                {
                    RoleModuleModel? roleModul = roleModuleInCache
                        .Where(a => a.RecordStatus.Equals(RecordStatusConstant.Active) && a.RoleModuleName.Equals(modulName))
                        .FirstOrDefault();

                    if (roleModul != null) isAllow = true;
                }
                else
                {
                    if (userId is null)
                    {
                        throw new KnownException(ErrorMessageConstant.DataNotFound);
                    }

                    UserModel? existingUser = await dbContext.User.Where(a => a.UserId == userId).SingleOrDefaultAsync();
                    if (existingUser is null)
                    {
                        throw new KnownException(ErrorMessageConstant.DataNotFound);
                    }

                    List<RoleModuleModel> roleModules = await dbContext.RoleModule
                        .Where(a => a.RecordStatus == RecordStatusConstant.Active && a.RoleId == existingUser.RoleId)
                        .ToListAsync();

                    // check existing role modul
                    RoleModuleModel? roleModul = roleModules.Where(a => a.RoleModuleName == modulName).FirstOrDefault();
                    
                    if (roleModul != null)
                    {
                        isAllow = true;

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetSlidingExpiration(TimeSpan.FromHours(1))
                            .SetAbsoluteExpiration(TimeSpan.FromHours(8))
                            .SetPriority(CacheItemPriority.Normal)
                            .SetSize(1);

                        // Don't Forget : Remove when role modul modified
                        // Don't Forget : Remove when user role modified
                        cacheService.SetData(MemoryCacheConstant.RoleModuleKey + userId, roleModules, TimeSpan.FromHours(8));
                    }
                }

                return isAllow;
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, "IsAllowAccessModule", userId);

                return false;
            }
        }
    }
}