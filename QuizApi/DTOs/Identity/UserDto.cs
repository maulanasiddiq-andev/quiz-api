using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizApi.DTOs.Identity
{
    public class UserDto : BaseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime? EmailVerifiedTime { get; set; }
        public string? ProfileImage { get; set; }
        public string? CoverImage { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public int FailedLoginAttempts { get; set; }
    }
}