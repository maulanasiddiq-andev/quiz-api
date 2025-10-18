using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models.Identity
{
    public class UserModel : BaseModel
    {
        [Key]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string HashedPassword { get; set; } = string.Empty;
        public string? RoleId { get; set; }
        public RoleModel? Role { get; set; }
        public DateTime? EmailVerifiedTime { get; set; } = null;
        public string? ProfileImage { get; set; }
        public string? CoverImage { get; set; }
        public DateTime? LastLoginTime { get; set; } = null;
        public int FailedLoginAttempts { get; set; } = 0;
    }
}