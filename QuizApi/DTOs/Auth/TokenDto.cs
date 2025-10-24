using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuizApi.DTOs.Identity;

namespace QuizApi.DTOs.Auth
{
    public class TokenDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiredTime { get; set; }
        public bool IsValidLogin { get; set; }
        public UserDto? User { get; set; }
    }
}