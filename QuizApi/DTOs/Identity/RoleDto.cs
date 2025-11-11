using FluentValidation;

namespace QuizApi.DTOs.Identity
{
    public class RoleDto : BaseDto
    {
        public RoleDto()
        {
            RoleModules = new();
        }

        public string RoleId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public List<RoleModuleDto> RoleModules { get; set; }
    }

    public class RoleAddValidator : AbstractValidator<RoleDto>
    {
        public RoleAddValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Nama role tidak boleh kosong");
        }
    }
}