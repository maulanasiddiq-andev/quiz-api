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
    }
}