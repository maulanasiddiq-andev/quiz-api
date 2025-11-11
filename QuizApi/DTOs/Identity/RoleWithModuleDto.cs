using FluentValidation;

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
        public bool IsMain { get; set; }
        public List<SelectModuleDto> RoleModules { get; set; }
    }
    
    public class RoleUpdateValidator : AbstractValidator<RoleWithModuleDto>
    {
        public RoleUpdateValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Nama role tidak boleh kosong");
            RuleFor(x => x.Version).NotNull().NotEmpty().WithMessage("Version role tidak boleh kosong");
        }
    }
}