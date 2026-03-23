using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace QuizApi.Models.Identity
{
    [FirestoreData]
    public class RoleModel : BaseModel
    {
        public RoleModel()
        {
            IsMain = false;
        }
        
        [FirestoreProperty]
        public string RoleId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string Name { get; set; } = string.Empty;
        [FirestoreProperty]
        public bool IsMain { get; set; }
    }
}