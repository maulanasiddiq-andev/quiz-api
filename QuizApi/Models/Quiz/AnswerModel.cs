using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models.Quiz
{
    public class AnswerModel : BaseModel
    {
        [Key]
        public string AnswerId { get; set; } = string.Empty;
        public string? QuestionId { get; set; }
        public int AnswerOrder { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsTrueAnswer { get; set; }
    }
}