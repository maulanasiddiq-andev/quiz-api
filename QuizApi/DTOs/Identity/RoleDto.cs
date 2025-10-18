using FluentValidation;

namespace QuizApi.DTOs.Identity
{
    public class RoleDto : BaseDto
    {
        public string RoleId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class RoleValidator : AbstractValidator<RoleDto>
    {
        public RoleValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Nama role tidak boleh kosong");
        }
    }
}