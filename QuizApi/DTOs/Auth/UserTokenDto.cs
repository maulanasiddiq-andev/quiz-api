namespace QuizApi.DTOs.Auth
{
    public class UserTokenDto
    {
        public string UserTokenId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiredTime { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiredTime { get; set; }
        public bool IsAccessAllowed { get; set; }     
    }
}