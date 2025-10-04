namespace QuizApi.DTOs.QuizExam
{
    public class QuestionExamDto
    {
        public QuestionExamDto()
        {
            Answers = new List<AnswerExamDto>();
        }
        public string Text { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public string? ImageUrl { get; set; }
        public List<AnswerExamDto> Answers { get; set; }
        public int SelectedAnswerOrder { get; set; }
        public bool IsAnswerTrue { get; set; }
    }
}