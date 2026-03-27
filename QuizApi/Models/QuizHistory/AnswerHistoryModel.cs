using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace QuizApi.Models.QuizHistory
{
    [FirestoreData]
    public class AnswerHistoryModel
    {
        [FirestoreProperty]
        public string AnswerHistoryId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string QuestionHistoryId { get; set; } = string.Empty;
        [FirestoreProperty]
        public int AnswerOrder { get; set; }
        [FirestoreProperty]
        public string? Text { get; set; }
        [FirestoreProperty]
        public string? ImageUrl { get; set; }
        [FirestoreProperty]
        public bool IsTrueAnswer { get; set; }     
    }
}