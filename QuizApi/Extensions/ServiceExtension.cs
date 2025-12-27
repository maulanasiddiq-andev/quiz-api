using QuizApi.Repositories;
using QuizApi.Services;
using RabbitMQ.Client;

namespace QuizApi.Extensions
{
    public static class ServiceExtension
    {
        public static void RegisterRepositories(this IServiceCollection collection)
        {
            collection.AddScoped<AuthRepository>();
            collection.AddScoped<UserRepository>();
            
            collection.AddScoped<CategoryRepository>();
            collection.AddScoped<HistoryRepository>();

            collection.AddScoped<QuizRepository>();

            collection.AddScoped<RoleRepository>();

            collection.AddScoped<ActivityLogService>();
            collection.AddScoped<CacheService>();
            collection.AddScoped<RoleModuleValidationService>();
            collection.AddScoped<PushNotificationService>();

            // queue service
            collection.AddSingleton(sp =>
            {
               return new ConnectionFactory
               {
                    HostName = "localhost",
                    UserName = "guest",
                    Password = "guest"
               }.CreateConnectionAsync().GetAwaiter().GetResult();
            });
            collection.AddSingleton<QueueService>();
        }
    }
}