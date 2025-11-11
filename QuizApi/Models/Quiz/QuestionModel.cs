using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [ForeignKey(nameof(QuizId))]
        public virtual QuizModel? Quiz { get; set; }
        public int QuestionOrder { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public virtual ICollection<AnswerModel> Answers { get; set; }     
    }
}