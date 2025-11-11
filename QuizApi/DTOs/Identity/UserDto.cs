using FluentValidation;

namespace QuizApi.DTOs.Identity
{
    public class UserDto : BaseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime? EmailVerifiedTime { get; set; }
        public string? ProfileImage { get; set; }
        public string? CoverImage { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public int FailedLoginAttempts { get; set; }
        public string? RoleId { get; set; }
        public RoleDto? Role { get; set; }
    }

    public class UserAddValidator : AbstractValidator<UserDto>
    {
        public UserAddValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email harus diisi");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password harus diisi");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Nama harus diisi");
        }
    }

    public class UserUpdateValidator : AbstractValidator<UserDto>
    {
        public UserUpdateValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email harus diisi");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Nama harus diisi");
            RuleFor(x => x.Version).NotNull().NotEmpty().WithMessage("Version harus diisi");
        }
    }
}