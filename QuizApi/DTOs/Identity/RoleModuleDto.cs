namespace QuizApi.DTOs.Identity
{
    public class RoleModuleDto : BaseDto
    {
        public string RoleModuleId { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public string RoleModuleName { get; set; } = string.Empty;
    }
}