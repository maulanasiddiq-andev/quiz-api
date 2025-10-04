namespace QuizApi.DTOs.QuizHistory
{
    public class QuestionHistoryDto
    {
        public QuestionHistoryDto()
        {
            Answers = new List<AnswerHistoryDto>();
        }
        public string QuestionHistoryId { get; set; } = string.Empty;
        public string QuizHistoryId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public string? ImageUrl { get; set; }
        public List<AnswerHistoryDto> Answers { get; set; }
        public int SelectedAnswerOrder { get; set; }
        public bool IsAnswerTrue { get; set; }
    }
}