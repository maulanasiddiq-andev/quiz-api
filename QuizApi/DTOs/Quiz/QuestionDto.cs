using FluentValidation;

namespace QuizApi.DTOs.Quiz
{
    public class QuestionDto : BaseDto
    {
        public QuestionDto()
        {
            Answers = new List<AnswerDto>();
        }

        public string QuestionId { get; set; } = string.Empty;
        public string? QuizId { get; set; }
        public int QuestionOrder { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<AnswerDto> Answers { get; set; }
    }

    public class QuestionValidator : AbstractValidator<QuestionDto>
    {
        public QuestionValidator()
        {
            RuleFor(x => x.Text).NotEmpty().WithMessage("Teks pertanyaan harus diisi");

            When(x => x.Answers is not null, () =>
            {
                RuleForEach(d => d.Answers).SetValidator(new AnswerValidator());
            });
        }
    }
}