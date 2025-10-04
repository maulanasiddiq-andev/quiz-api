namespace QuizApi.DTOs.QuizExam
{
    public class AnswerExamDto
    {
        public int AnswerOrder { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsTrueAnswer { get; set; }        
    }
}