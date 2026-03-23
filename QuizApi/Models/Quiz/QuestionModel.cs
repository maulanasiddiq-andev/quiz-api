using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Google.Cloud.Firestore;

namespace QuizApi.Models.Quiz
{
    [FirestoreData]
    public class QuestionModel : BaseModel
    {
        public QuestionModel()
        {
            Answers = new List<AnswerModel>();    
        }

        [FirestoreProperty]
        public string QuestionId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string? QuizId { get; set; }
        [ForeignKey(nameof(QuizId))]
        public virtual QuizModel? Quiz { get; set; }
        [FirestoreProperty]
        public int QuestionOrder { get; set; }
        [FirestoreProperty]
        public string Text { get; set; } = string.Empty;
        [FirestoreProperty]
        public string? ImageUrl { get; set; }
        public virtual ICollection<AnswerModel> Answers { get; set; }     
    }
}