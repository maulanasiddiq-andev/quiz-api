namespace QuizApi.DTOs.Auth
{
    public class CheckOtpDto
    {
        public string Email { get; set; } = string.Empty;
        public int OtpCode { get; set; }
    }
}