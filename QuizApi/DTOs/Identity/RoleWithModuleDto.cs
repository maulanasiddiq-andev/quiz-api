namespace QuizApi.DTOs.Identity
{
    public class RoleWithModuleDto : BaseDto
    {
        public RoleWithModuleDto()
        {
            RoleModules = new();
        }
        
        public string RoleId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<SelectModuleDto> RoleModules { get; set; }
    }
}