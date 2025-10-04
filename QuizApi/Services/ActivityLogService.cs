using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace QuizApi.Services
{
    public class ActivityLogService
    {
        private readonly ActivityLogDBContext _dBContext;
        public ActivityLogService(ActivityLogDBContext dBContext)
        {
            _dBContext = dBContext;
        }

        public void SaveUserActivityLog(
            string action,
            string? userId,
            string? fromObject = null,
            string? toObject = null,
            string refId = ""
        )
        {
            var task = Task.Run(async () =>
            {

                try
                {
                    UserActivityLogModel userActivityLog = new()
                    {
                        UserActivityLogId = Guid.NewGuid().ToString("N"),
                        UserId = userId ?? "",
                        Action = action,
                        ReferenceKeyId = refId,
                        ActionLevel = "OK",
                        UtcDate = DateTime.UtcNow,
                        FromJsonObject = fromObject,
                        ToJsonObject = toObject
                    };


                    _dBContext.Add(userActivityLog);
                    await _dBContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    SaveErrorLog(ex, action, userId);
                }
            });

            task.Wait();
        }

        public void SaveErrorLog(
            Exception ex,
            string action,
            string? userId,
            string? fromObject = null,
            string? toObject = null,
            string errorLevel = "Basic"
        )
        {

            string? message = ex?.Message;
            string? strakTrace = ex?.StackTrace;

            try
            {
                if (ex?.InnerException != null)
                {
                    message += " " + ex.InnerException.Message;
                    strakTrace += " " + ex.InnerException.StackTrace;

                    if (ex.InnerException.InnerException != null)
                    {
                        message += " " + ex.InnerException.InnerException.Message;
                        strakTrace += " " + ex.InnerException.InnerException.StackTrace;
                    }
                }
            }
            catch { }


            var task = Task.Run(async () =>
            {
                try
                {
                    ErrorActivityLogModel errorLog = new()
                    {
                        ErrorActivityLogId = Guid.NewGuid().ToString("N"),
                        StackTrace = strakTrace,
                        ErrorLevel = errorLevel,
                        UserId = userId ?? "",
                        Action = action,
                        Message = message,
                        IsResolved = false,
                        FromJsonObject = fromObject,
                        ToJsonObject = toObject
                    };

                    _dBContext.Add(errorLog);
                    await _dBContext.SaveChangesAsync();
                }
                catch
                {

                }
            });

            task.Wait();
        }
    }

    public class ActivityLogDBContext : DbContext
    {
        private readonly bool isConfigured = false;

        public ActivityLogDBContext()
        {
            isConfigured = false;
        }

        public ActivityLogDBContext(DbContextOptions<ActivityLogDBContext> options) : base(options)
        {
            isConfigured = true;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!isConfigured)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

                string? connectionString = configuration.GetConnectionString("ActivityLogPostgreSql");
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                optionsBuilder.UseNpgsql(connectionString: connectionString);
            }
        }

        public DbSet<ErrorActivityLogModel> ErrorLog { get; set; }
        public DbSet<UserActivityLogModel> UserActivityLog { get; set; }
    }

    public class ErrorActivityLogModel
    {
        public ErrorActivityLogModel()
        {
            IsResolved = false;
            UtcDate = DateTime.UtcNow;
            ErrorLevel = "Basic";
        }

        [Key]
        public string ErrorActivityLogId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime UtcDate { get; set; }
        // Related to ModulConstant
        public string? Action { get; set; }
        //Basic, Fatal
        public string? ErrorLevel { get; set; }
        public string? Message { get; set; }
        public string? StackTrace { get; set; }
        public string? FromJsonObject { get; set; }
        public string? ToJsonObject { get; set; }
        public bool IsResolved { get; set; }
    }


    public class UserActivityLogModel
    {
        public UserActivityLogModel()
        {
            UtcDate = DateTime.UtcNow;
            ActionLevel = "OK";
            Status = "OK";
        }

        [Key]
        public string UserActivityLogId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime UtcDate { get; set; }
        public string? Action { get; set; }
        public string? Description { get; set; }
        public string? FromJsonObject { get; set; }
        public string? ToJsonObject { get; set; }
        // Related To Table Key
        public string? ReferenceKeyId { get; set; }
        public string? ReferenceTable { get; set; }
        public string? Status { get; set; }
        // OK, Danger, Very Danger
        public string? ActionLevel { get; set; }
    }
}