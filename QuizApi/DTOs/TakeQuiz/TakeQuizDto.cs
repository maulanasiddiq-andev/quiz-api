namespace QuizApi.DTOs.TakeQuiz
{
    public class TakeQuizDto
    {
        public TakeQuizDto()
        {
            Questions = new List<TakeQuestionDto>();
        }

        public string QuizId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? Time { get; set; }
        public List<TakeQuestionDto> Questions { get; set; }
        public int QuestionCount { get; set; }
        public uint Version { get; set; }
    }
}