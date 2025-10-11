namespace QuizApi.DTOs.CheckQuiz
{
    public class CheckQuizDto
    {
        public CheckQuizDto()
        {
            Questions = new List<CheckQuestionDto>();
        }
        public uint QuizVersion { get; set; }
        public List<CheckQuestionDto> Questions { get; set; }
        public int QuestionCount { get; set; }        
        public int Duration { get; set; }        
        public int TrueAnswers { get; set; }        
        public int WrongAnswers { get; set; }        
        public int Score { get; set; }        
    }
}