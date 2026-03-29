using Google.Cloud.Firestore;

namespace QuizApi.Models.Identity
{
    [FirestoreData]
    public class FcmTokenModel : BaseModel
    {
        [FirestoreProperty]
        public string FcmTokenId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string Token { get; set; } = string.Empty;
        [FirestoreProperty]
        public string UserId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string Device { get; set; } = string.Empty;
    }
}