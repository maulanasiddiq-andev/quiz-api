namespace QuizApi.DTOs.QuizExam
{
    public class QuizExamDto
    {
        public QuizExamDto()
        {
            Questions = new List<QuestionExamDto>();
        }
        public uint QuizVersion { get; set; }
        public List<QuestionExamDto> Questions { get; set; }
        public int QuestionCount { get; set; }        
        public int Duration { get; set; }        
        public int TrueAnswers { get; set; }        
        public int WrongAnswers { get; set; }        
        public int Score { get; set; }        
    }
}