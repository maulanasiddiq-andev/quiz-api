namespace QuizApi.DTOs.TakeQuiz
{
    public class TakeQuestionDto
    {
        public TakeQuestionDto()
        {
            Answers = new List<TakeAnswerDto>();
        }

        public string QuestionId { get; set; } = string.Empty;
        public string? QuizId { get; set; }
        public int QuestionOrder { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<TakeAnswerDto> Answers { get; set; }
    }
}