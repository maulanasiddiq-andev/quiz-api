using FluentValidation;

namespace QuizApi.DTOs.Auth
{
    public class RegisterDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterValidator : AbstractValidator<RegisterDto>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Nama tidak boleh kosong");
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email tidak boleh kosong");
            RuleFor(x => x.Email).EmailAddress().WithMessage("Alamat email harus valid");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password tidak boleh kosong");
        }
    }
}