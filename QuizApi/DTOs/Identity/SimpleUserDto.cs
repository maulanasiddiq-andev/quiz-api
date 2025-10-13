namespace QuizApi.DTOs.Identity
{
    public class SimpleUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public string? CoverImage { get; set; }
    }
}