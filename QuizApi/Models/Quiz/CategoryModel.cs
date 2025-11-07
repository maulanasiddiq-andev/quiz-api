using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models.Quiz
{
    public class CategoryModel : BaseModel
    {
        [Key]
        public string CategoryId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsMain { get; set; }
    }
}