using FluentValidation;

namespace QuizApi.DTOs.Quiz
{
    public class CategoryDto : BaseDto
    {
        public string CategoryId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class CategoryValidator : AbstractValidator<CategoryDto>
    {
        public CategoryValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Nama kategori harus diisi");
        }
    }

    public class CategoryUpdateValidator : AbstractValidator<CategoryDto>
    {
        public CategoryUpdateValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Nama kategori harus diisi");
            RuleFor(x => x.Version).NotNull().WithMessage("Versi tidak boleh kosong");
        }
    }
}