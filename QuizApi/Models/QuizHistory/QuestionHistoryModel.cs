using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models.QuizHistory
{
    public class QuestionHistoryModel
    {
        public QuestionHistoryModel()
        {
            Answers = new List<AnswerHistoryModel>();
        }

        [Key]
        public string QuestionHistoryId { get; set; } = string.Empty;
        public string QuizHistoryId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public string? ImageUrl { get; set; }
        public List<AnswerHistoryModel> Answers { get; set; }
        public int? SelectedAnswerOrder { get; set; }
        public bool IsAnswerTrue { get; set; }
    }
}