namespace QuizApi.DTOs.CheckQuiz
{
    public class CheckQuestionDto
    {
        public int QuestionOrder { get; set; }
        public int? SelectedAnswerOrder { get; set; }
    }
}