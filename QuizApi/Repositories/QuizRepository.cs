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
using QuizApi.DTOs.Identity;
using QuizApi.Helpers;
using QuizApi.Services;
using QuizApi.Models.Identity;

namespace QuizApi.Repositories
{
    public class QuizRepository
    {
        private readonly QuizAppDBContext dBContext;
        private readonly IMapper mapper;
        private readonly string userId = "";
        private readonly ActionModelHelper actionModelHelper;
        private readonly PushNotificationService pushNotificationService;
        public QuizRepository(
            QuizAppDBContext dBContext,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            PushNotificationService pushNotificationService
        )
        {
            this.dBContext = dBContext;
            this.mapper = mapper;
            this.pushNotificationService = pushNotificationService;
            actionModelHelper = new ActionModelHelper();

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        public async Task<SearchResponse> SearchDatasAsync(QuizFilterDto searchRequest)
        {
            IQueryable<QuizDto> listQuizzesQuery = dBContext.Quiz
                .Where(x => x.RecordStatus == RecordStatusConstant.Active)
                .Select(x => new QuizDto
                {
                    QuizId = x.QuizId,
                    Title = x.Title,
                    Description = x.Description,
                    ImageUrl = x.ImageUrl,
                    CategoryId = x.CategoryId,
                    Category = mapper.Map<CategoryDto>(x.Category),
                    Time = x.Time,
                    UserId = x.UserId,
                    CreatedBy = x.CreatedBy,
                    CreatedTime = x.CreatedTime,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedTime = x.ModifiedTime,
                    Version = x.Version,
                    RecordStatus = x.RecordStatus,
                    QuestionCount = x.Questions.Count(),
                    HistoriesCount = x.Histories.Count(),
                    // check if the current user has taken the quiz
                    IsTakenByUser = x.Histories.Any(y => y.UserId == userId)
                });

            #region Query
            if (!string.IsNullOrWhiteSpace(searchRequest.Search))
            {
                listQuizzesQuery = listQuizzesQuery.Where(x => EF.Functions.ILike(x.Title, $"%{searchRequest.Search}%"));
            }
            
            if (!string.IsNullOrEmpty(searchRequest.CategoryId))
            {
                listQuizzesQuery = listQuizzesQuery.Where(x => x.CategoryId == searchRequest.CategoryId).AsQueryable();
            }
            #endregion

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

            response.Items = listQuiz;

            return response;
        }

        public async Task CreateDataAsync(QuizDto quizDto)
        {
            QuizModel quiz = mapper.Map<QuizModel>(quizDto);

            quiz.UserId = userId;
            actionModelHelper.AssignCreateModel(quiz, "Quiz", userId);

            int questionOrder = 0;
            foreach (var question in quiz.Questions)
            {
                question.QuizId = quiz.QuizId;
                question.QuestionOrder = questionOrder;
                actionModelHelper.AssignCreateModel(question, "Question", userId);

                int answerOrder = 0;
                foreach (var answer in question.Answers)
                {
                    answer.QuestionId = question.QuestionId;
                    answer.AnswerOrder = answerOrder;
                    actionModelHelper.AssignCreateModel(answer, "Answer", userId);

                    answerOrder++;
                }

                questionOrder++;
            }

            await dBContext.AddAsync(quiz);
            await dBContext.SaveChangesAsync();
        }

        public async Task<QuizDto> GetDataByIdAsync(string id)
        {
            QuizDto? quiz = await dBContext.Quiz
                .Where(x => x.QuizId.Equals(id) && x.RecordStatus == RecordStatusConstant.Active)
                .Select(x => new QuizDto
                {
                    QuizId = x.QuizId,
                    Title = x.Title,
                    Description = x.Description,
                    ImageUrl = x.ImageUrl,
                    CategoryId = x.CategoryId,
                    Category = mapper.Map<CategoryDto>(x.Category),
                    Time = x.Time,
                    UserId = x.UserId,
                    User = mapper.Map<SimpleUserDto>(x.User),
                    CreatedBy = x.CreatedBy,
                    CreatedTime = x.CreatedTime,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedTime = x.ModifiedTime,
                    Version = x.Version,
                    RecordStatus = x.RecordStatus,
                    QuestionCount = x.Questions.Count(),
                    HistoriesCount = x.Histories.Count(),
                    // check if the current user has taken the quiz
                    IsTakenByUser = x.Histories.Any(y => y.UserId == userId)
                })
                .FirstOrDefaultAsync();

            if (quiz is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            return quiz;
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

            // check if user has taken the quiz
            // if yes, throw
            if (quiz.Histories.Any(x => x.UserId == userId))
            {
                throw new KnownException("Anda sudah mengerjakan kuis ini");
            }

            TakeQuizDto quizDto = mapper.Map<TakeQuizDto>(quiz);

            foreach (var question in quizDto.Questions)
            {
                question.Answers = question.Answers.OrderBy(x => x.AnswerOrder).ToList();
            }

            quizDto.QuestionCount = quizDto.Questions.Count();

            return quizDto;
        }

        public async Task DeleteDataAsync(string id)
        {
            QuizModel? quiz = await dBContext.Quiz.Where(x => x.QuizId == id && x.RecordStatus == RecordStatusConstant.Active).FirstOrDefaultAsync();

            if (quiz is null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            // only creator of the quiz can remove the quiz
            if (quiz.UserId != userId)
            {
                throw new KnownException("Anda tidak diizinkan mengakses fitur ini");
            }

            actionModelHelper.AssignDeleteModel(quiz, userId);

            dBContext.Update(quiz);
            await dBContext.SaveChangesAsync();
        }

        public async Task<QuizHistoryModel> CheckQuizAsync(CheckQuizDto checkQuizDto, string quizId)
        {
            QuizModel? quiz = await dBContext.Quiz
                .Where(x => x.QuizId == quizId && x.RecordStatus == RecordStatusConstant.Active)
                .Include(x => x.Questions).ThenInclude(y => y.Answers)
                .FirstOrDefaultAsync();

            if (quiz == null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            // map quiz model to quiz history model, then check the answers with check quiz dto
            QuizHistoryModel quizHistory = mapper.Map<QuizHistoryModel>(quiz);

            quizHistory.QuizId = quizId;
            quizHistory.QuizVersion = checkQuizDto.QuizVersion;
            quizHistory.UserId = userId;
            actionModelHelper.AssignCreateModel(quizHistory, "QuizHistory", userId);

            // check every question from the quiz
            foreach (var question in quizHistory.Questions)
            {
                question.QuizHistoryId = quizHistory.QuizHistoryId;
                actionModelHelper.AssignCreateModel(question, "QuestionHistory", userId);

                // question about to be checked
                CheckQuestionDto? checkQuestion = checkQuizDto.Questions.Where(x => x.QuestionOrder == question.QuestionOrder).FirstOrDefault();

                // if the checked question is not found, the quiz is not valid
                if (checkQuestion == null)
                {
                    throw new KnownException(ErrorMessageConstant.DataNotFound);
                }
                else
                {
                    AnswerHistoryModel? selectedAnswer = question.Answers.Where(x => x.AnswerOrder == checkQuestion.SelectedAnswerOrder).FirstOrDefault();

                    // if user didn't answer the question
                    // automatically assign it as false
                    if (selectedAnswer == null)
                    {
                        question.SelectedAnswerOrder = null;
                        question.IsAnswerTrue = false;
                    }
                    else
                    {
                        question.SelectedAnswerOrder = checkQuestion.SelectedAnswerOrder;
                        question.IsAnswerTrue = selectedAnswer.IsTrueAnswer;
                    }
                }

                // assign id for answers
                foreach (var answer in question.Answers)
                {
                    answer.QuestionHistoryId = question.QuestionHistoryId;
                    actionModelHelper.AssignCreateModel(answer, "AnswerHistory", userId);
                }

                question.Answers = question.Answers.OrderBy(x => x.AnswerOrder).ToList();
            }

            // quiz history metadata
            quizHistory.Questions = quizHistory.Questions.OrderBy(x => x.QuestionOrder).ToList();
            quizHistory.QuizVersion = checkQuizDto.QuizVersion;
            quizHistory.Duration = checkQuizDto.Duration;
            quizHistory.QuestionCount = checkQuizDto.QuestionCount;
            quizHistory.TrueAnswers = quizHistory.Questions.Where(x => x.IsAnswerTrue).Count();
            quizHistory.WrongAnswers = quizHistory.QuestionCount - quizHistory.TrueAnswers;
            quizHistory.Score = (int)Math.Round((double)quizHistory.TrueAnswers / quizHistory.QuestionCount * 100);

            await dBContext.AddAsync(quizHistory);
            await dBContext.SaveChangesAsync();

            // send push notification for the creator
            // the notification is sent to all devices related to the creator
            List<FcmTokenModel> fcmTokens = await dBContext.FcmToken
                .Where(x => x.UserId == quiz.UserId && x.RecordStatus == RecordStatusConstant.Active)
                .ToListAsync();
            // if there are fcm tokens (one or more)
            if (fcmTokens.Any())
            {
                UserModel? quizTaker = await dBContext.User.Where(x => x.UserId == userId).FirstOrDefaultAsync();
                if (quizTaker != null)
                {
                    foreach (var fcmToken in fcmTokens)
                    {
                        await pushNotificationService.SendNotificationAsync(
                            fcmToken.Token,
                            "Kuis Dikerjakan",
                            $"{quizTaker.Name} telah mengerjakan kuis anda yang berjudul \"{quiz.Title}\""
                        );   
                    }   
                }
            }

            return quizHistory;
        }

        public async Task<SearchResponse> GetHistoriesByQuizIdAsync(SearchRequestDto searchRequest, string quizId)
        {
            IQueryable<QuizHistoryModel> listQuizHistoriesQuery = dBContext.QuizHistory
                .Where(x => x.RecordStatus == RecordStatusConstant.Active && x.QuizId.Equals(quizId))
                .Include(x => x.User)
                .Include(x => x.Quiz)
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