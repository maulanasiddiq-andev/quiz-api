using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace QuizApi.Models.Auth
{
    [FirestoreData]
    public class OtpModel : BaseModel
    {
        [FirestoreProperty]
        public string OtpId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string Email { get; set; } = string.Empty;
        [FirestoreProperty]
        public int OtpCode { get; set; }
        [FirestoreProperty]
        public DateTime ExpiredTime { get; set; } = DateTime.UtcNow.AddMinutes(15);
    }
}