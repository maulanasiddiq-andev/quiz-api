namespace QuizApi.DTOs.Auth
{
    public class LoginWithGoogleDto
    {
        public string IdToken { get; set; } = string.Empty;
        public string? FcmToken { get; set; }
        public string? Device { get; set; }
    }
}