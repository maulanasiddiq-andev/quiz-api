using System.ComponentModel.DataAnnotations;
using QuizApi.Models.Identity;
using QuizApi.Models.Quiz;

namespace QuizApi.Models.QuizHistory
{
    public class QuizHistoryModel : BaseModel
    {
        public QuizHistoryModel()
        {
            Questions = new List<QuestionHistoryModel>();
        }

        [Key]
        public string QuizHistoryId { get; set; } = string.Empty;
        public string QuizId { get; set; } = string.Empty;
        public QuizModel? Quiz { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Time { get; set; }
        public uint QuizVersion { get; set; }
        public string UserId { get; set; } = string.Empty;
        public UserModel? User { get; set; }
        public List<QuestionHistoryModel> Questions { get; set; }
        public int QuestionCount { get; set; }        
        public int Duration { get; set; }        
        public int TrueAnswers { get; set; }        
        public int WrongAnswers { get; set; }        
        public int Score { get; set; }  
    }
}