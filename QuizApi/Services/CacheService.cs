using Microsoft.Extensions.Caching.Memory;
using QuizApi.Extensions;

namespace QuizApi.Services
{
    public class CacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly string userId = "";
        public CacheService(
            IMemoryCache memoryCache,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _memoryCache = memoryCache;

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        public T? GetData<T>(string key)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out T? value))
                {
                    return value;
                }
            }
            catch (Exception)
            {
                // activityLogService.SaveErrorLog(ex, "GetData", userId, tenantId, key);
            }

            return default;
        }

        public bool SetData<T>(string key, T value, TimeSpan expirationTime)
        {
            try
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime
                };

                _memoryCache.Set(key, value, cacheEntryOptions);
                return true;
            }
            catch (Exception)
            {
                // activityLogService.SaveErrorLog(ex, "SetData", userId, tenantId, key);
            }
            return false;
        }

        public void RemoveData(string key)
        {
            try
            {
                _memoryCache.Remove(key);
            }
            catch (Exception)
            {
                // activityLogService.SaveErrorLog(ex, "RemoveData", userId, tenantId, key);
            }
        }

        // public void RemoveUserRelatedCache(string userId)
        // {
        //     try
        //     {
        //         RemoveData(MemoryCacheConstant.UserTokenKey + userId);
        //         RemoveData(MemoryCacheConstant.RoleModulKey + userId);
        //         RemoveData(MemoryCacheConstant.UserKey + userId);
        //         RemoveData(MemoryCacheConstant.UserRoleKey + userId);
        //     }
        //     catch { }
        // }
        
        // public void RemoveUserRelatedCache(List<string> usersId)
        // {
        //     try
        //     {
        //         foreach (var userId in usersId)
        //         {
        //             RemoveUserRelatedCache(userId);
        //         }
        //     }
        //     catch { }
        // }
    }
}