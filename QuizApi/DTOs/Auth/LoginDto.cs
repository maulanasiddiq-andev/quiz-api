using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace QuizApi.DTOs.Auth
{
    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginValidator : AbstractValidator<LoginDto>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email tidak boleh kosong");
            RuleFor(x => x.Email).EmailAddress().WithMessage("Alamat email harus valid");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password tidak boleh kosong");
        }
    }
}