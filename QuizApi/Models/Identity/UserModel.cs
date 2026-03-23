using Google.Cloud.Firestore;

namespace QuizApi.Models.Identity
{
    [FirestoreData]
    public class UserModel : BaseModel
    {
        [FirestoreProperty]
        public string UserId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string Name { get; set; } = string.Empty;
        [FirestoreProperty]
        public string Username { get; set; } = string.Empty;
        [FirestoreProperty]
        public string Email { get; set; } = string.Empty;
        [FirestoreProperty]
        public string HashedPassword { get; set; } = string.Empty;
        [FirestoreProperty]
        public string? RoleId { get; set; }
        [FirestoreProperty]
        public RoleModel? Role { get; set; }
        [FirestoreProperty]
        public DateTime? EmailVerifiedTime { get; set; } = null;
        [FirestoreProperty]
        public string? ProfileImage { get; set; }
        [FirestoreProperty]
        public string? CoverImage { get; set; }
        [FirestoreProperty]
        public DateTime? LastLoginTime { get; set; } = null;
        [FirestoreProperty]
        public int FailedLoginAttempts { get; set; } = 0;
    }
}