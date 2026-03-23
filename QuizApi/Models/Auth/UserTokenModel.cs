using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace QuizApi.Models.Auth
{
    [FirestoreData]
    public class UserTokenModel : BaseModel
    {
        public UserTokenModel()
        {
            RefreshTokenExpiredTime = DateTime.UtcNow;    
        }

        [FirestoreProperty]
        public string UserTokenId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string UserId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string Token { get; set; } = string.Empty;
        [FirestoreProperty]
        public bool IsAccessAllowed { get; set; }
        [FirestoreProperty]
        public DateTime ExpiredTime { get; set; }
        [FirestoreProperty]
        public string? RefreshToken { get; set; }
        [FirestoreProperty]
        public DateTime RefreshTokenExpiredTime { get; set; }
        [FirestoreProperty]
        public string? Browser { get; set; }   
        [FirestoreProperty]
        public string? Device { get; set; }   
        [FirestoreProperty]
        public string? OsVersion { get; set; }   
        [FirestoreProperty]
        public string? UserAgent { get; set; }   
        [FirestoreProperty]
        public string? Location { get; set; }   
        [FirestoreProperty]
        public double? LocationLatitude { get; set; }   
        [FirestoreProperty]
        public double? LocationLongitude { get; set; }   
    }
}