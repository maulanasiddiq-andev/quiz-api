namespace QuizApi.DTOs.TakeQuiz
{
    public class TakeAnswerDto
    {
        public string AnswerId { get; set; } = string.Empty;
        public string? QuestionId { get; set; }
        public int AnswerOrder { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; } 
    }
}