using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Google.Cloud.Firestore;

namespace QuizApi.Models.Quiz
{
    [FirestoreData]
    public class AnswerModel : BaseModel
    {
        [FirestoreProperty]
        public string AnswerId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string? QuestionId { get; set; }
        public virtual QuestionModel? Question { get; set; }
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