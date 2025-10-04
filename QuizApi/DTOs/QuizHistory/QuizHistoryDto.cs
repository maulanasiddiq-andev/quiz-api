using QuizApi.DTOs.Identity;
using QuizApi.DTOs.Quiz;

namespace QuizApi.DTOs.QuizHistory
{
    public class QuizHistoryDto : BaseDto
    {
        public QuizHistoryDto()
        {
            Questions = new List<QuestionHistoryDto>();
        }
        public string QuizHistoryId { get; set; } = string.Empty;
        public string QuizId { get; set; } = string.Empty;
        public QuizDto? Quiz { get; set; }
        public uint QuizVersion { get; set; }
        public string UserId { get; set; } = string.Empty;
        public UserDto? User { get; set; }
        public List<QuestionHistoryDto> Questions { get; set; }
        public int QuestionCount { get; set; }        
        public int Duration { get; set; }        
        public int TrueAnswers { get; set; }        
        public int WrongAnswers { get; set; }        
        public int Score { get; set; }
    }
}