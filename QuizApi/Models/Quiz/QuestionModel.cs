using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models.Quiz
{
    public class QuestionModel : BaseModel
    {
        public QuestionModel()
        {
            Answers = new List<AnswerModel>();    
        }

        [Key]
        public string QuestionId { get; set; } = string.Empty;
        public string? QuizId { get; set; }
        public int QuestionOrder { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public virtual ICollection<AnswerModel> Answers { get; set; }     
    }
}