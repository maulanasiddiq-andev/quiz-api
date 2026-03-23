using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace QuizApi.Models.Identity
{
    [FirestoreData]
    public class RoleModuleModel : BaseModel
    {
        [FirestoreProperty]
        public string RoleModuleId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string RoleId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string RoleModuleName { get; set; } = string.Empty;
    }
}