namespace QuizApi.DTOs.QuizHistory
{
    public class AnswerHistoryDto
    {
        public string AnswerHistoryId { get; set; } = string.Empty;
        public string QuestionHistoryId { get; set; } = string.Empty;
        public int AnswerOrder { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsTrueAnswer { get; set; } 
    }
}