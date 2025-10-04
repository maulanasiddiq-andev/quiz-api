using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuizApi.Constants;
using QuizApi.DTOs.QuizHistory;
using QuizApi.Exceptions;
using QuizApi.Models;
using QuizApi.Models.QuizHistory;

namespace QuizApi.Repositories
{
    public class HistoryRepository
    {
        private readonly QuizAppDBContext dBContext;
        private readonly IMapper mapper;
        public HistoryRepository(QuizAppDBContext dBContext, IMapper mapper)
        {
            this.dBContext = dBContext;
            this.mapper = mapper;
        }

        public async Task<QuizHistoryDto> GetDataByIdAsync(string id)
        {
            QuizHistoryModel? quizHistory = await dBContext.QuizHistory
                .Where(x => x.QuizHistoryId.Equals(id) && x.RecordStatus == RecordStatusConstant.Active)
                .Select(MapQuizHistoryWithQuestions)
                .FirstOrDefaultAsync();

            if (quizHistory == null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            QuizHistoryDto quizHistoryDto = mapper.Map<QuizHistoryDto>(quizHistory);

            return quizHistoryDto;
        }

        private Expression<Func<QuizHistoryModel, QuizHistoryModel>> MapQuizHistoryWithQuestions = quiz => new QuizHistoryModel
        {
            CreatedBy = quiz.CreatedBy,
            CreatedTime = quiz.CreatedTime,
            DeletedBy = quiz.DeletedBy,
            DeletedTime = quiz.DeletedTime,
            Description = quiz.Description,
            Duration = quiz.Duration,
            ModifiedBy = quiz.ModifiedBy,
            ModifiedTime = quiz.ModifiedTime,
            QuestionCount = quiz.QuestionCount,
            Questions = quiz.Questions
                .OrderBy(x => x.QuestionOrder)
                .Select(x => new QuestionHistoryModel
                {
                    QuestionHistoryId = x.QuestionHistoryId,
                    QuizHistoryId = quiz.QuizHistoryId,
                    Text = x.Text,
                    ImageUrl = x.ImageUrl,
                    IsAnswerTrue = x.IsAnswerTrue,
                    QuestionOrder = x.QuestionOrder,
                    SelectedAnswerOrder = x.SelectedAnswerOrder,
                    Answers = x.Answers.OrderBy(y => y.AnswerOrder).Select(y => y).ToList()
                })
                .ToList(),
            QuizHistoryId = quiz.QuizHistoryId,
            QuizId = quiz.QuizId,
            QuizVersion = quiz.QuizVersion,
            RecordStatus = quiz.RecordStatus,
            Score = quiz.Score,
            TrueAnswers = quiz.TrueAnswers,
            User = quiz.User,
            UserId = quiz.UserId,
            Version = quiz.Version,
            WrongAnswers = quiz.WrongAnswers
        };
    }
}