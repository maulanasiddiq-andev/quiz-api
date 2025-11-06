using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models.Identity
{
    public class FcmTokenModel : BaseModel
    {
        [Key]
        public string FcmTokenId { get; set; } = string.Empty;
        [Required]
        public string Token { get; set; } = string.Empty;
        [Required]
        public string UserId { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
    }
}