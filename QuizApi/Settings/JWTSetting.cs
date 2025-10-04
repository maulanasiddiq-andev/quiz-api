using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizApi.Settings
{
    public class JWTSetting
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int RefreshTokenExpiredTimeInHour { get; set; }
        public int TokenExpiredTimeInHour { get; set; }
    }
}