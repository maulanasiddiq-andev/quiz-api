using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using QuizApi.Constants;
using QuizApi.Extensions;

namespace QuizApi.Services
{
    public class CacheService
    {
        private readonly IDistributedCache cache;
        private readonly ActivityLogService activityLogService;
        private readonly string userId = "";
        public CacheService(
            IDistributedCache cache,
            IHttpContextAccessor httpContextAccessor,
            ActivityLogService activityLogService
        )
        {
            this.cache = cache;
            this.activityLogService = activityLogService;

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        public async Task<T?> GetDataAsync<T>(string key)
        {
            try
            {
                var data = await cache.GetStringAsync(key);
                if (data == null)
                    return default;

                return JsonSerializer.Deserialize<T>(data);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, "GetData", userId, key);
                return default;
            }
        }

        public async Task<bool> SetDataAsync<T>(string key, T value, TimeSpan expirationTime)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime
                };

                var json = JsonSerializer.Serialize(value);
                await cache.SetStringAsync(key, json, options);

                return true;
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, "SetData", userId, key);
                return false;
            }
        }

        public async Task RemoveDataAsync(string key)
        {
            try
            {
                await cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, "RemoveData", userId, key);
            }
        }

        public async Task RemoveUserRelatedCache(string userId)
        {
            try
            {
                await RemoveDataAsync(MemoryCacheConstant.UserTokenKey + userId);
                await RemoveDataAsync(MemoryCacheConstant.RoleModuleKey + userId);
                await RemoveDataAsync(MemoryCacheConstant.UserKey + userId);
            }
            catch { }
        }
        
        public async Task RemoveUserRelatedCache(List<string> usersId)
        {
            try
            {
                foreach (var userId in usersId)
                {
                    await RemoveDataAsync(MemoryCacheConstant.UserTokenKey + userId);
                    await RemoveDataAsync(MemoryCacheConstant.RoleModuleKey + userId);
                    await RemoveDataAsync(MemoryCacheConstant.UserKey + userId);
                }
            }
            catch { }
        }
    }
}