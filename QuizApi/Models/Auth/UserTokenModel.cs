using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace QuizApi.Models.Auth
{
    public class UserTokenModel : BaseModel
    {
        public UserTokenModel()
        {
            RefreshTokenExpiredTime = DateTime.UtcNow;    
        }

        [Key]
        public string UserTokenId { get; set; } = string.Empty;
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string Token { get; set; } = string.Empty;
        public bool IsAccessAllowed { get; set; }
        public DateTime ExpiredTime { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiredTime { get; set; }
        public string? Browser { get; set; }   
        public string? Device { get; set; }   
        public string? OsVersion { get; set; }   
        public string? UserAgent { get; set; }   
        public string? Location { get; set; }   
        public double? LocationLatitude { get; set; }   
        public double? LocationLongitude { get; set; }   
    }
}