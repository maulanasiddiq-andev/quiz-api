using System.ComponentModel.DataAnnotations;
using QuizApi.Models.Identity;
using QuizApi.Models.QuizHistory;

namespace QuizApi.Models.Quiz
{
    public class QuizModel : BaseModel
    {
        public QuizModel()
        {
            Questions = new List<QuestionModel>();
            Histories = new List<QuizHistoryModel>();
        }

        [Key]
        public string QuizId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public UserModel? User { get; set; }
        public string? CategoryId { get; set; }
        public CategoryModel? Category { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Time { get; set; }
        public int QuestionCount { get; set; }
        public virtual ICollection<QuestionModel> Questions { get; set; }
        public int HistoriesCount { get; set; }
        public virtual ICollection<QuizHistoryModel> Histories { get; set; }
    }
}