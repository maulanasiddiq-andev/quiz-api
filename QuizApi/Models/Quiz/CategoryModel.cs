using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace QuizApi.Models.Quiz
{
    [FirestoreData]
    public class CategoryModel : BaseModel
    {
        [FirestoreProperty]
        public string CategoryId { get; set; } = string.Empty;
        [FirestoreProperty]
        public string Name { get; set; } = string.Empty;
        [FirestoreProperty]
        public bool IsMain { get; set; }
    }
}