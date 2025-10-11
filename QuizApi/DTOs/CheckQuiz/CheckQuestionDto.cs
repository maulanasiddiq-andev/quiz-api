namespace QuizApi.DTOs.CheckQuiz
{
    public class CheckQuestionDto
    {
        public CheckQuestionDto()
        {
            Answers = new List<CheckAnswerDto>();
        }
        public string Text { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public string? ImageUrl { get; set; }
        public List<CheckAnswerDto> Answers { get; set; }
        public int SelectedAnswerOrder { get; set; }
        public bool IsAnswerTrue { get; set; }
    }
}