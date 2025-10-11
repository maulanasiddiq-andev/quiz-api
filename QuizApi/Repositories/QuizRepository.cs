using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuizApi.Constants;
using QuizApi.DTOs.Quiz;
using QuizApi.DTOs.CheckQuiz;
using QuizApi.DTOs.QuizHistory;
using QuizApi.DTOs.Request;
using QuizApi.DTOs.TakeQuiz;
using QuizApi.Exceptions;
using QuizApi.Extensions;
using QuizApi.Models;
using QuizApi.Models.Quiz;
using QuizApi.Models.QuizHistory;
using QuizApi.Responses;

namespace QuizApi.Repositories
{
    public class QuizRepository
    {
        private readonly QuizAppDBContext dBContext;
        private readonly IMapper mapper;
        private readonly string userId = "";
        public QuizRepository(
            QuizAppDBContext dBContext,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor
        )
        {
            this.dBContext = dBContext;
            this.mapper = mapper;

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        public async Task<SearchResponse> SearchDatasAsync(SearchRequestDto searchRequest)
        {
            IQueryable<QuizModel> listQuizzesQuery = dBContext.Quiz
                .Where(x => x.RecordStatus == RecordStatusConstant.Active)
                .Select(MapQuiz);

            #region Ordering
            string orderBy = searchRequest.OrderBy;
            string orderDir = searchRequest.OrderDir;

            if (orderBy.Equals("createdTime"))
            {
                if (orderDir.Equals("asc"))
                {
                    listQuizzesQuery = listQuizzesQuery.OrderBy(x => x.CreatedTime).AsQueryable();
                }
                else if (orderDir.Equals("desc"))
                {
                    listQuizzesQuery = listQuizzesQuery.OrderByDescending(x => x.CreatedTime).AsQueryable();
                }
            }
            #endregion

            var response = new SearchResponse();
            response.TotalItems = await listQuizzesQuery.CountAsync();
            response.CurrentPage = searchRequest.CurrentPage;
            response.PageSize = searchRequest.PageSize;

            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var take = searchRequest.PageSize;
            var listQuiz = await listQuizzesQuery.Skip(skip).Take(take).ToListAsync();

            response.Items = mapper.Map<List<QuizDto>>(listQuiz);

            return response;
        }

        public async Task CreateDataAsync(QuizDto quizDto)
        {
            QuizModel quiz = mapper.Map<QuizModel>(quizDto);

            quiz.QuizId = Guid.NewGuid().ToString("N");
            quiz.UserId = userId;
            quiz.CreatedTime = DateTime.UtcNow;
            quiz.ModifiedTime = DateTime.UtcNow;
            quiz.CreatedBy = userId;
            quiz.ModifiedBy = userId;
            quiz.RecordStatus = RecordStatusConstant.Active;

            int questionOrder = 0;
            foreach (var question in quiz.Questions)
            {
                question.QuestionId = Guid.NewGuid().ToString("N");
                question.QuizId = quiz.QuizId;
                question.QuestionOrder = questionOrder;
                question.CreatedTime = DateTime.UtcNow;
                question.ModifiedTime = DateTime.UtcNow;
                question.CreatedBy = userId;
                question.ModifiedBy = userId;
                question.RecordStatus = RecordStatusConstant.Active;

                int answerOrder = 0;
                foreach (var answer in question.Answers)
                {
                    answer.AnswerId = Guid.NewGuid().ToString("N");
                    answer.QuestionId = question.QuestionId;
                    answer.AnswerOrder = answerOrder;
                    answer.CreatedTime = DateTime.UtcNow;
                    answer.ModifiedTime = DateTime.UtcNow;
                    answer.CreatedBy = userId;
                    answer.ModifiedBy = userId;
                    answer.RecordStatus = RecordStatusConstant.Active;

                    answerOrder++;
                }

                questionOrder++;
            }

            await dBContext.AddAsync(quiz);
            await dBContext.SaveChangesAsync();
        }

        public async Task<QuizDto> GetDataByIdAsync(string id)
        {
            QuizModel? quiz = await GetActiveQuizByIdAsync(id);

            if (quiz is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            QuizDto quizDto = mapper.Map<QuizDto>(quiz);

            return quizDto;
        }

        public async Task<TakeQuizDto> GetQuizWithQuestionsByIdAsync(string quizId)
        {
            QuizModel? quiz = await dBContext.Quiz
                .Where(x => x.QuizId == quizId && x.RecordStatus == RecordStatusConstant.Active)
                .Include(x => x.Questions.OrderBy(y => y.QuestionOrder)).ThenInclude(y => y.Answers)
                .FirstOrDefaultAsync();

            if (quiz == null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            TakeQuizDto quizDto = mapper.Map<TakeQuizDto>(quiz);

            foreach (var question in quizDto.Questions)
            {
                question.Answers = question.Answers.OrderBy(x => x.AnswerOrder).ToList();
            }

            quizDto.QuestionsCount = quizDto.Questions.Count();

            return quizDto;
        }

        public async Task DeleteDataAsync(string id)
        {
            QuizModel? quiz = await GetActiveQuizByIdAsync(id);

            if (quiz is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            quiz.RecordStatus = RecordStatusConstant.Deleted;

            dBContext.Update(quiz);
            await dBContext.SaveChangesAsync();
        }

        private async Task<QuizModel?> GetActiveQuizByIdAsync(string id)
        {
            QuizModel? quiz = await dBContext.Quiz
                .Where(x => x.QuizId.Equals(id) && x.RecordStatus == RecordStatusConstant.Active)
                .Select(MapQuiz)
                .FirstOrDefaultAsync();

            return quiz;
        }

        // private Expression<Func<QuizModel, QuizModel>> MapQuizWithQuestions = quiz => new QuizModel
        // {
        //     QuizId = quiz.QuizId,
        //     Category = quiz.Category,
        //     CategoryId = quiz.CategoryId,
        //     CreatedBy = quiz.CreatedBy,
        //     CreatedTime = quiz.CreatedTime,
        //     DeletedBy = quiz.DeletedBy,
        //     DeletedTime = quiz.DeletedTime,
        //     Description = quiz.Description,
        //     ImageUrl = quiz.ImageUrl,
        //     ModifiedBy = quiz.ModifiedBy,
        //     ModifiedTime = quiz.ModifiedTime,
        //     RecordStatus = quiz.RecordStatus,
        //     Time = quiz.Time,
        //     Title = quiz.Title,
        //     User = quiz.User,
        //     UserId = quiz.UserId,
        //     Version = quiz.Version,
        //     Questions = quiz.Questions
        //         .Where(q => q.RecordStatus == RecordStatusConstant.Active)
        //         .OrderBy(x => x.QuestionOrder)
        //         .Select(q => new QuestionModel
        //         {
        //             QuestionId = q.QuestionId,
        //             QuizId = q.QuizId,
        //             Text = q.Text,
        //             RecordStatus = q.RecordStatus,
        //             Answers = q.Answers
        //                 .Where(a => a.RecordStatus == RecordStatusConstant.Active)
        //                 .OrderBy(a => a.AnswerOrder)
        //                 .Select(a => a)
        //                 .ToList(),
        //             CreatedBy = q.CreatedBy,
        //             QuestionOrder = q.QuestionOrder,
        //             CreatedTime = q.CreatedTime,
        //             DeletedBy = q.DeletedBy,
        //             DeletedTime = q.DeletedTime,
        //             Description = q.Description,
        //             ImageUrl = q.ImageUrl,
        //             ModifiedBy = q.ModifiedBy,
        //             ModifiedTime = q.ModifiedTime,
        //             Version = q.Version
        //         })
        //         .ToList(),
        //     QuestionsCount = quiz.Questions.Count,
        //     HistoriesCount = quiz.Histories.Count
        // };

        private Expression<Func<QuizModel, QuizModel>> MapQuiz = quiz => new QuizModel
        {
            QuizId = quiz.QuizId,
            Category = quiz.Category,
            CategoryId = quiz.CategoryId,
            CreatedBy = quiz.CreatedBy,
            CreatedTime = quiz.CreatedTime,
            DeletedBy = quiz.DeletedBy,
            DeletedTime = quiz.DeletedTime,
            Description = quiz.Description,
            ImageUrl = quiz.ImageUrl,
            ModifiedBy = quiz.ModifiedBy,
            ModifiedTime = quiz.ModifiedTime,
            RecordStatus = quiz.RecordStatus,
            Time = quiz.Time,
            Title = quiz.Title,
            User = quiz.User,
            UserId = quiz.UserId,
            Version = quiz.Version,
            QuestionsCount = quiz.Questions.Count,
            HistoriesCount = quiz.Histories.Count
        };

        public async Task TakeQuizAsync(CheckQuizDto quizExamDto, string quizId)
        {
            QuizHistoryModel quizHistory = mapper.Map<QuizHistoryModel>(quizExamDto);

            quizHistory.QuizHistoryId = Guid.NewGuid().ToString("N");
            quizHistory.QuizId = quizId;
            quizHistory.QuizVersion = quizExamDto.QuizVersion;
            quizHistory.UserId = userId;
            quizHistory.CreatedBy = userId;
            quizHistory.CreatedTime = DateTime.UtcNow;
            quizHistory.Description = "";
            quizHistory.ModifiedBy = userId;
            quizHistory.ModifiedTime = DateTime.UtcNow;
            quizHistory.RecordStatus = RecordStatusConstant.Active;

            foreach (var questionHistory in quizHistory.Questions)
            {
                questionHistory.QuestionHistoryId = Guid.NewGuid().ToString("N");
                questionHistory.QuizHistoryId = quizHistory.QuizHistoryId;

                foreach (var answerHistory in questionHistory.Answers)
                {
                    answerHistory.AnswerHistoryId = Guid.NewGuid().ToString("N");
                    answerHistory.QuestionHistoryId = questionHistory.QuestionHistoryId;
                }
            }

            await dBContext.AddAsync(quizHistory);
            await dBContext.SaveChangesAsync();
        }

        public async Task<SearchResponse> GetHistoriesByQuizIdAsync(SearchRequestDto searchRequest, string quizId)
        {
            IQueryable<QuizHistoryModel> listQuizHistoriesQuery = dBContext.QuizHistory
                .Where(x => x.RecordStatus == RecordStatusConstant.Active && x.QuizId.Equals(quizId))
                .Include(x => x.User)
                .AsQueryable();

            #region Ordering
            string orderBy = searchRequest.OrderBy;
            string orderDir = searchRequest.OrderDir;

            if (orderBy.Equals("createdTime"))
            {
                if (orderDir.Equals("asc"))
                {
                    listQuizHistoriesQuery = listQuizHistoriesQuery.OrderBy(x => x.CreatedTime).AsQueryable();
                }
                else if (orderDir.Equals("desc"))
                {
                    listQuizHistoriesQuery = listQuizHistoriesQuery.OrderByDescending(x => x.CreatedTime).AsQueryable();
                }
            }
            if (orderBy.Equals("score"))
            {
                if (orderDir.Equals("asc"))
                {
                    listQuizHistoriesQuery = listQuizHistoriesQuery.OrderBy(x => x.Score).AsQueryable();
                }
                else if (orderDir.Equals("desc"))
                {
                    listQuizHistoriesQuery = listQuizHistoriesQuery.OrderByDescending(x => x.Score).AsQueryable();
                }
            }
            #endregion

            var response = new SearchResponse();
            response.TotalItems = await listQuizHistoriesQuery.CountAsync();
            response.CurrentPage = searchRequest.CurrentPage;
            response.PageSize = searchRequest.PageSize;

            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var take = searchRequest.PageSize;
            var listQuizHistories = await listQuizHistoriesQuery.Skip(skip).Take(take).ToListAsync();

            response.Items = mapper.Map<List<QuizHistoryDto>>(listQuizHistories);

            return response;
        }
    }
}