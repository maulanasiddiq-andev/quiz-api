using Google.Cloud.Firestore;
using QuizApi.Constants;
using QuizApi.Models.Identity;
using QuizApi.Models;

namespace QuizApi.Services
{
    public class RoleModuleValidationService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ActivityLogService _activityLogService;

        public RoleModuleValidationService(
            [FromKeyedServices("quiz-db")] FirestoreDb firestoreDb, 
            ActivityLogService logService
        )
        {
            _firestoreDb = firestoreDb;
            _activityLogService = logService;
        }

        public async Task<bool> IsAllowAccessModuleAsync(string userId, string moduleName)
        {
            try
            {
                // 1. Get the User to find their RoleId
                Query query = _firestoreDb.Collection("user").WhereEqualTo("UserId", userId).Limit(1);
                QuerySnapshot userSnap = await query.GetSnapshotAsync();

                if (!userSnap.Documents.Any()) return false;

                // Extract RoleId directly to avoid full model conversion if preferred
                string? roleId = userSnap.Documents.First().GetValue<string>("RoleId");
                if (string.IsNullOrEmpty(roleId)) return false;

                // 2. Check if a RoleModule exists for this Role and Module Name
                // Collection name should match your Firestore setup (lowercase recommended)
                Query moduleQuery = _firestoreDb.Collection("rolemodule")
                    .WhereEqualTo("RoleId", roleId)
                    .WhereEqualTo("RoleModuleName", moduleName)
                    .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                    .Limit(1);

                QuerySnapshot moduleSnap = await moduleQuery.GetSnapshotAsync();

                return moduleSnap.Documents.Count > 0;
            }
            catch (Exception ex)
            {
                _activityLogService.SaveErrorLog(ex, "IsAllowAccessModuleFirestore", userId);
                return false;
            }
        }
    }
}