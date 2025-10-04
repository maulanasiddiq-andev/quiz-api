using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models.Auth
{
    public class OtpModel : BaseModel
    {
        [Key]
        public string OtpId { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public int OtpCode { get; set; }
        public DateTime ExpiredTime { get; set; } = DateTime.UtcNow.AddMinutes(15);
    }
}