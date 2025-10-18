using Microsoft.Extensions.Caching.Memory;
using QuizApi.Constants;
using QuizApi.Extensions;

namespace QuizApi.Services
{
    public class CacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ActivityLogService activityLogService;
        private readonly string userId = "";
        public CacheService(
            IMemoryCache memoryCache,
            IHttpContextAccessor httpContextAccessor,
            ActivityLogService activityLogService
        )
        {
            _memoryCache = memoryCache;
            this.activityLogService = activityLogService;

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
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, "GetData", userId, key);
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
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, "SetData", userId, key);
            }
            return false;
        }

        public void RemoveData(string key)
        {
            try
            {
                _memoryCache.Remove(key);
            }
            catch (Exception ex)
            {
                activityLogService.SaveErrorLog(ex, "RemoveData", userId, key);
            }
        }

        public void RemoveUserRelatedCache(string userId)
        {
            try
            {
                RemoveData(MemoryCacheConstant.UserTokenKey + userId);
                RemoveData(MemoryCacheConstant.RoleModuleKey + userId);
                RemoveData(MemoryCacheConstant.UserKey + userId);
            }
            catch { }
        }
        
        public void RemoveUserRelatedCache(List<string> usersId)
        {
            try
            {
                foreach (var userId in usersId)
                {
                    RemoveData(MemoryCacheConstant.UserTokenKey + userId);
                    RemoveData(MemoryCacheConstant.RoleModuleKey + userId);
                    RemoveData(MemoryCacheConstant.UserKey + userId);
                }
            }
            catch { }
        }
    }
}