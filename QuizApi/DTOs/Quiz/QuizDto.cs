using FluentValidation;
using QuizApi.DTOs.Identity;

namespace QuizApi.DTOs.Quiz
{
    public class QuizDto : BaseDto
    {
        public QuizDto()
        {
            Questions = new List<QuestionDto>();
        }

        public string QuizId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public SimpleUserDto? User { get; set; }
        public string? CategoryId { get; set; }
        public CategoryDto? Category { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? Time { get; set; }
        public List<QuestionDto> Questions { get; set; }
        public int QuestionCount { get; set; }
        public int HistoriesCount { get; set; }
    }

    public class QuizValidator : AbstractValidator<QuizDto>
    {
        public QuizValidator()
        {
            RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Kategori harus diisi");
            RuleFor(x => x.Title).NotEmpty().WithMessage("Judul harus diisi");
            RuleFor(x => x.Time)
                .NotNull().WithMessage("Waktu tidak boleh kosong")
                .Must(x => x > 0).WithMessage("Waktu tidak boleh kosong");

            When(x => x.Questions is not null, () =>
            {
                RuleForEach(d => d.Questions).SetValidator(new QuestionValidator());
            });
        }
    }
}