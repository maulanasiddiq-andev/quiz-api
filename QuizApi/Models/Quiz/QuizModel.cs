using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;
using QuizApi.Models.Identity;
using QuizApi.Models.QuizHistory;

namespace QuizApi.Models.Quiz
{
    [FirestoreData]
    public class QuizModel : BaseModel
    {
        public QuizModel()
        {
            Questions = new List<QuestionModel>();
            Histories = new List<QuizHistoryModel>();
        }

        [FirestoreProperty]
        public string QuizId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string? UserId { get; set; }
        [FirestoreProperty]
        public UserModel? User { get; set; }
        [FirestoreProperty]
        public string? CategoryId { get; set; }
        [FirestoreProperty]
        public CategoryModel? Category { get; set; }
        [FirestoreProperty]
        public string Title { get; set; } = string.Empty;
        [FirestoreProperty]
        public string? ImageUrl { get; set; }
        [FirestoreProperty]
        public int Time { get; set; }
        [FirestoreProperty]
        public int QuestionCount { get; set; }
        public virtual ICollection<QuestionModel> Questions { get; set; }
        [FirestoreProperty]
        public int HistoriesCount { get; set; }
        public virtual ICollection<QuizHistoryModel> Histories { get; set; }
    }
}