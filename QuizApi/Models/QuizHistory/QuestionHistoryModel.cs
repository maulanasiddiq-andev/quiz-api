using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace QuizApi.Models.QuizHistory
{
    [FirestoreData]
    public class QuestionHistoryModel
    {
        public QuestionHistoryModel()
        {
            Answers = new List<AnswerHistoryModel>();
        }

        [FirestoreProperty]
        public string QuestionHistoryId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string QuizHistoryId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string Text { get; set; } = string.Empty;
        [FirestoreProperty]
        public int QuestionOrder { get; set; }
        [FirestoreProperty]
        public string? ImageUrl { get; set; }
        public List<AnswerHistoryModel> Answers { get; set; }
        [FirestoreProperty]
        public int? SelectedAnswerOrder { get; set; }
        [FirestoreProperty]
        public bool IsAnswerTrue { get; set; }
    }
}