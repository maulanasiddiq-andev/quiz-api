using Google.Cloud.Firestore;
using QuizApi.Models.Identity;
using QuizApi.Models.Quiz;

namespace QuizApi.Models.QuizHistory
{
    [FirestoreData]
    public class QuizHistoryModel : BaseModel
    {
        public QuizHistoryModel()
        {
            Questions = new List<QuestionHistoryModel>();
        }

        [FirestoreProperty]
        public string QuizHistoryId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string QuizId { get; set; } = string.Empty;
        public QuizModel? Quiz { get; set; }
        [FirestoreProperty]
        public string Title { get; set; } = string.Empty;
        [FirestoreProperty]
        public string? ImageUrl { get; set; }
        [FirestoreProperty]
        public int Time { get; set; }
        [FirestoreProperty]
        public Timestamp QuizVersion { get; set; }
        [FirestoreProperty]
        public string UserId { get; set; } = string.Empty;
        public UserModel? User { get; set; }
        public List<QuestionHistoryModel> Questions { get; set; }
        [FirestoreProperty]
        public int QuestionCount { get; set; }
        [FirestoreProperty]        
        public int Duration { get; set; }
        [FirestoreProperty]        
        public int TrueAnswers { get; set; }
        [FirestoreProperty]        
        public int WrongAnswers { get; set; }
        [FirestoreProperty]        
        public int Score { get; set; }  
    }
}