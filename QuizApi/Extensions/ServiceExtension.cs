using QuizApi.Repositories;
using QuizApi.Services;

namespace QuizApi.Extensions
{
    public static class ServiceExtension
    {
        public static void RegisterRepositories(this IServiceCollection collection)
        {
            collection.AddScoped<AuthRepository>();
            collection.AddScoped<CategoryRepository>();
            collection.AddScoped<HistoryRepository>();

            collection.AddScoped<QuizRepository>();

            collection.AddScoped<RoleRepository>();

            collection.AddScoped<ActivityLogService>();
            collection.AddScoped<CacheService>();
            collection.AddScoped<RoleModuleValidationService>();
        }
    }
}