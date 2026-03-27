using Google.Cloud.Firestore;

namespace QuizApi.DTOs.CheckQuiz
{
    public class CheckQuizDto
    {
        public CheckQuizDto()
        {
            Questions = new List<CheckQuestionDto>();
        }
        public Timestamp QuizVersion { get; set; }
        public List<CheckQuestionDto> Questions { get; set; }
        public int QuestionCount { get; set; }        
        public int Duration { get; set; }       
    }
}