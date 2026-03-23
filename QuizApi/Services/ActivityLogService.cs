using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;

namespace QuizApi.Services
{
    public class ActivityLogService
    {
        private readonly FirestoreDb firestoreDb;
        private readonly ActivityLogDBContext _dBContext;
        public ActivityLogService(ActivityLogDBContext dBContext, [FromKeyedServices("quiz-db-activity-log")] FirestoreDb firestoreDb)
        {
            this.firestoreDb = firestoreDb;
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
            // Start the task in the background and move on immediately
            _ = Task.Run(async () =>
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

                    await firestoreDb.Collection("useractivitylog").AddAsync(userActivityLog);
                }
                catch (Exception ex)
                {
                    // Fallback to error logging if the activity log fails
                    // Note: Since this is already in a Task.Run, call the logic directly 
                    // or ensure SaveErrorLog also handles its own background threading.
                    SaveErrorLog(ex, action, userId);
                }
            });
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
            string message = ex?.ToString() ?? "Unknown Error";
            string? stackTrace = ex?.StackTrace;

            // We start the task but do NOT call .Wait()
            // This allows the LoginAsync method to finish and return the response immediately
            _ = Task.Run(async () =>
            {
                try
                {
                    ErrorActivityLogModel errorLog = new()
                    {
                        ErrorActivityLogId = Guid.NewGuid().ToString("N"),
                        StackTrace = stackTrace,
                        ErrorLevel = errorLevel,
                        UserId = userId ?? "",
                        Action = action,
                        Message = message,
                        IsResolved = false,
                        FromJsonObject = fromObject,
                        ToJsonObject = toObject,
                        UtcDate = DateTime.UtcNow
                    };

                    await firestoreDb.Collection("erroractivitylog").AddAsync(errorLog);
                }
                catch (Exception firestoreEx)
                {
                    // Since this is in a background thread, we log to console 
                    // so we can see if it fails in the server logs.
                    Console.WriteLine($"Logging failed: {firestoreEx.Message}");
                }
            });
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

    [FirestoreData]
    public class ErrorActivityLogModel
    {
        public ErrorActivityLogModel()
        {
            IsResolved = false;
            UtcDate = DateTime.UtcNow;
            ErrorLevel = "Basic";
        }

        [FirestoreProperty]
        public string ErrorActivityLogId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string UserId { get; set; } = string.Empty;
        [FirestoreProperty]
        public DateTime UtcDate { get; set; }
        // Related to ModulConstant
        [FirestoreProperty]
        public string? Action { get; set; }
        //Basic, Fatal
        [FirestoreProperty]
        public string? ErrorLevel { get; set; }
        [FirestoreProperty]
        public string? Message { get; set; }
        [FirestoreProperty]
        public string? StackTrace { get; set; }
        [FirestoreProperty]
        public string? FromJsonObject { get; set; }
        [FirestoreProperty]
        public string? ToJsonObject { get; set; }
        [FirestoreProperty]
        public bool IsResolved { get; set; }
    }


    [FirestoreData]
    public class UserActivityLogModel
    {
        public UserActivityLogModel()
        {
            UtcDate = DateTime.UtcNow;
            ActionLevel = "OK";
            Status = "OK";
        }

        [FirestoreProperty]
        public string UserActivityLogId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string UserId { get; set; } = string.Empty;
        [FirestoreProperty]
        public DateTime UtcDate { get; set; }
        [FirestoreProperty]
        public string? Action { get; set; }
        [FirestoreProperty]
        public string? Description { get; set; }
        [FirestoreProperty]
        public string? FromJsonObject { get; set; }
        [FirestoreProperty]
        public string? ToJsonObject { get; set; }
        // Related To Table Key
        [FirestoreProperty]
        public string? ReferenceKeyId { get; set; }
        [FirestoreProperty]
        public string? ReferenceTable { get; set; }
        [FirestoreProperty]
        public string? Status { get; set; }
        // OK, Danger, Very Danger
        [FirestoreProperty]
        public string? ActionLevel { get; set; }
    }
}