using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models.Identity
{
    public class RoleModel : BaseModel
    {
        public RoleModel()
        {
            IsMain = false;
        }
        
        [Key]
        public string RoleId { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;
        public bool IsMain { get; set; }
    }
}