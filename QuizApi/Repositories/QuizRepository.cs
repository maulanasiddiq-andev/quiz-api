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
using QuizApi.Queues;

namespace QuizApi.Repositories
{
    public class QuizRepository
    {
        private readonly QuizAppDBContext dBContext;
        private readonly IMapper mapper;
        private readonly string userId = "";
        private readonly ActionModelHelper actionModelHelper;
        // for updating quiz
        private readonly CategoryRepository categoryRepository;
        private readonly QueueService queueService;
        public QuizRepository(
            QuizAppDBContext dBContext,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            CategoryRepository categoryRepository,
            QueueService queueService
        )
        {
            this.dBContext = dBContext;
            this.mapper = mapper;
            this.categoryRepository = categoryRepository;
            this.queueService = queueService;
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
                    QuestionCount = x.Questions.Count(q => q.RecordStatus == RecordStatusConstant.Active),
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
                    QuestionCount = x.Questions.Where(q => q.RecordStatus == RecordStatusConstant.Active).Count(),
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

        public async Task<TakeQuizDto> TakeQuizByIdAsync(string quizId)
        {
            QuizModel? quiz = await dBContext.Quiz
                .Where(x => x.QuizId == quizId && x.RecordStatus == RecordStatusConstant.Active)
                .Select(MapQuizWithQuestions)
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
        
        public async Task<QuizDto> GetQuizWithQuestionsByIdAsync(string quizId)
        {            
            QuizModel? quiz = await dBContext.Quiz
                .Where(x => x.QuizId == quizId && x.RecordStatus == RecordStatusConstant.Active)
                .Select(MapQuizWithQuestions)
                .FirstOrDefaultAsync();

            if (quiz == null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            QuizDto quizDto = mapper.Map<QuizDto>(quiz);

            // get category for showing it in edit page
            if (quizDto.CategoryId != null)
            {
                quizDto.Category = await categoryRepository.GetDataByIdAsync(quizDto.CategoryId);
            }

            return quizDto;
        }

        public async Task<QuizDto> UpdateDataByIdAsync(string id, QuizDto quizDto)
        {
            QuizModel? quiz = await dBContext.Quiz
                .Where(x => x.QuizId == id && x.RecordStatus == RecordStatusConstant.Active)
                .Select(MapQuizWithQuestions)
                .FirstOrDefaultAsync();

            if (quiz == null)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            if (quiz.UserId != userId)
            {
                throw new KnownException(ErrorMessageConstant.AccessNotAllowed);
            }

            // update the quiz
            quiz.CategoryId = quizDto.CategoryId;
            quiz.Title = quizDto.Title;
            quiz.ImageUrl = quizDto.ImageUrl;
            quiz.Time = quizDto.Time ?? 0; // Time is nullable in dto

            actionModelHelper.AssignUpdateModel(quiz, userId);

            // update the questions
            foreach (var questionDto in quizDto.Questions)
            {
                // find the question that is about to be updated
                QuestionModel? question = quiz.Questions.Where(q => q.QuestionId == questionDto.QuestionId).FirstOrDefault();

                // if question is found, update
                if (question != null)
                {
                    question.Text = questionDto.Text;
                    question.ImageUrl = questionDto.ImageUrl;
                    question.QuestionOrder = questionDto.QuestionOrder;

                    actionModelHelper.AssignUpdateModel(question, userId);

                    // update the answers
                    foreach (var answerDto in questionDto.Answers)
                    {
                        // find the answer that is about to be updated
                        AnswerModel? answer = question.Answers.Where(a => a.AnswerId == answerDto.AnswerId).FirstOrDefault();

                        // if the answer is found, update
                        if (answer != null)
                        {
                            answer.Text = answerDto.Text;
                            answer.AnswerOrder = answerDto.AnswerOrder;
                            answer.ImageUrl = answerDto.ImageUrl;
                            answer.IsTrueAnswer = answerDto.IsTrueAnswer;

                            actionModelHelper.AssignUpdateModel(answer, userId);
                        }
                        // if the answer is not found
                        // answerDto is a new answer, add
                        else
                        {
                            AnswerModel newAnswer = mapper.Map<AnswerModel>(answerDto);
                            newAnswer.QuestionId = question.QuestionId;

                            actionModelHelper.AssignCreateModel(newAnswer, "Answer", userId);

                            // insert to question
                            // this action is prevented by concurrecy
                            // question.Answers.Add(newAnswer);

                            // add
                            await dBContext.AddAsync(newAnswer);
                        }
                    }

                    // check old answers that dont exist in questionDto
                    foreach (var answer in question.Answers)
                    {
                        AnswerDto? answerDto = questionDto.Answers.Where(x => x.AnswerId == answer.AnswerId).FirstOrDefault();

                        // if answer is not found in questionDto
                        // delete
                        if (answerDto == null)
                        {
                            actionModelHelper.AssignDeleteModel(answer, userId);
                        }
                    }
                }
                // if the question is not found
                // add
                else
                {
                    QuestionModel newQuestion = mapper.Map<QuestionModel>(questionDto);
                    newQuestion.QuizId = quiz.QuizId;

                    actionModelHelper.AssignCreateModel(newQuestion, "Question", userId);

                    // create the answers
                    foreach (var answer in newQuestion.Answers)
                    {
                        answer.QuestionId = newQuestion.QuestionId;
                        actionModelHelper.AssignCreateModel(answer, "Answer", userId);
                    }

                    // insert to quiz
                    // quiz.Questions.Add(newQuestion);

                    await dBContext.AddAsync(newQuestion);
                }
            }

            // check old questions that dont exist in quizDto
            foreach (var question in quiz.Questions)
            {
                QuestionDto? questionDto = quizDto.Questions.Where(q => q.QuestionId == question.QuestionId).FirstOrDefault();

                // if old question is not found in quizDto
                // delete
                if (questionDto == null)
                {
                    actionModelHelper.AssignDeleteModel(question, userId);
                }
            }

            dBContext.Update(quiz);
            await dBContext.SaveChangesAsync();

            return mapper.Map<QuizDto>(quiz);
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
                .Select(MapQuizWithQuestions)
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
                    var notificationQueue = new NotificationQueue
                    {
                        FcmTokens = fcmTokens.Select(x => x.Token).ToList(),
                        Title = "Kuis Anda Dikerjakan",
                        Body = $"{quizTaker.Name} telah mengerjakan kuis Anda: {quiz.Title}"
                    };

                    await queueService.Publish(QueueConstant.NotificationQueue, notificationQueue);   
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

        private Expression<Func<QuizModel, QuizModel>> MapQuizWithQuestions = quiz => new QuizModel
        {
            QuizId = quiz.QuizId,
            Title = quiz.Title,
            Description = quiz.Description,
            ImageUrl = quiz.ImageUrl,
            CategoryId = quiz.CategoryId,
            Time = quiz.Time,
            UserId = quiz.UserId,
            CreatedBy = quiz.CreatedBy,
            CreatedTime = quiz.CreatedTime,
            ModifiedBy = quiz.ModifiedBy,
            ModifiedTime = quiz.ModifiedTime,
            Version = quiz.Version,
            RecordStatus = quiz.RecordStatus,
            Questions = quiz.Questions
                .Where(q => q.RecordStatus == RecordStatusConstant.Active)
                .OrderBy(q => q.QuestionOrder)
                .Select(q => new QuestionModel
                {
                    QuestionId = q.QuestionId,
                    QuizId = q.QuizId,
                    Text = q.Text,
                    ImageUrl = q.ImageUrl,
                    QuestionOrder = q.QuestionOrder,
                    CreatedBy = q.CreatedBy,
                    CreatedTime = q.CreatedTime,
                    ModifiedBy = q.ModifiedBy,
                    ModifiedTime = q.ModifiedTime,
                    Version = q.Version,
                    RecordStatus = q.RecordStatus,
                    Answers = q.Answers
                        .Where(a => a.RecordStatus == RecordStatusConstant.Active)
                        .OrderBy(a => a.AnswerOrder)
                        .Select(a => new AnswerModel
                        {
                            AnswerId = a.AnswerId,
                            QuestionId = a.QuestionId,
                            Text = a.Text,
                            ImageUrl = a.ImageUrl,
                            AnswerOrder = a.AnswerOrder,
                            IsTrueAnswer = a.IsTrueAnswer,
                            CreatedBy = a.CreatedBy,
                            CreatedTime = a.CreatedTime,
                            ModifiedBy = a.ModifiedBy,
                            ModifiedTime = a.ModifiedTime,
                            Version = a.Version,
                            RecordStatus = a.RecordStatus
                        }).ToList()
                }).ToList(),
        };
    }
}