namespace QuizApi.DTOs.Request
{
    public class QuizFilterDto : SearchRequestDto
    {
        public string CategoryId { get; set; } = string.Empty;
    }
}