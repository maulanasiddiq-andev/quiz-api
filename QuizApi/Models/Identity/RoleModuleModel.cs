using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models.Identity
{
    public class RoleModuleModel : BaseModel
    {
        [Key]
        public string RoleModuleId { get; set; } = string.Empty;
        [Required]
        public string RoleId { get; set; } = string.Empty;
        public string RoleModuleName { get; set; } = string.Empty;
    }
}