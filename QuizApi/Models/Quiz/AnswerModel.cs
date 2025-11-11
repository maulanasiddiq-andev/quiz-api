using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuizApi.Models.Quiz
{
    public class AnswerModel : BaseModel
    {
        [Key]
        public string AnswerId { get; set; } = string.Empty;
        public string? QuestionId { get; set; }
        [ForeignKey(nameof(QuestionId))]
        public virtual QuestionModel? Question { get; set; }
        public int AnswerOrder { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsTrueAnswer { get; set; }
    }
}