using FluentValidation;

namespace QuizApi.DTOs.Quiz
{
    public class AnswerDto : BaseDto
    {
        public string AnswerId { get; set; } = string.Empty;
        public string? QuestionId { get; set; }
        public int AnswerOrder { get; set; }
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsTrueAnswer { get; set; }
    }

    public class AnswerValidator : AbstractValidator<AnswerDto>
    {
        public AnswerValidator()
        {
            RuleFor(x => x)
                .Must(y => string.IsNullOrWhiteSpace(y.Text) || string.IsNullOrWhiteSpace(y.ImageUrl))
                .WithMessage("Jawaban harus berupa teks atau gambar");
        }
    }
}