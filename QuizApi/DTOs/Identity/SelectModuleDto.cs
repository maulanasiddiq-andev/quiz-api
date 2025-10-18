namespace QuizApi.DTOs.Identity
{
    public class SelectModuleDto
    {
        public string RoleModuleName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}