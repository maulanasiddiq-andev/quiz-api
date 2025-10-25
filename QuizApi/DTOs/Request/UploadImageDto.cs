using FluentValidation;

namespace QuizApi.DTOs.Request
{
    public class UploadImageDto
    {
        public IFormFile? Image { get; set; }
        public string Directory { get; set; } = "";
    }

    public class UploadImageValidator : AbstractValidator<UploadImageDto>
    {
        public UploadImageValidator()
        {
            RuleFor(x => x.Image)
                .NotNull().WithMessage("Gambar tidak boleh kosong")
                .Must(y => y?.Length > 0).WithMessage("Gambar tidak boleh kosong");
            RuleFor(x => x.Directory).NotNull().WithMessage("Directory tidak boleh kosong");
        }
    }
}