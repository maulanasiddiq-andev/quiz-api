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
using QuizApi.Helpers;
using QuizApi.Models.Identity;
using QuizApi.Queues;
using Google.Cloud.Firestore;

namespace QuizApi.Repositories
{
    public class QuizRepository
    {
        private readonly FirestoreDb firestoreDb;
        private readonly IMapper mapper;
        private readonly string userId = "";
        private readonly ActionModelHelper actionModelHelper;
        // for updating quiz
        private readonly CategoryRepository categoryRepository;
        private readonly UserRepository userRepository;
        public QuizRepository(
            [FromKeyedServices("quiz-db")] FirestoreDb firestoreDb,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            CategoryRepository categoryRepository,
            UserRepository userRepository
        )
        {
            this.firestoreDb = firestoreDb;
            this.mapper = mapper;
            this.categoryRepository = categoryRepository;
            this.userRepository = userRepository;
            actionModelHelper = new ActionModelHelper();

            if (httpContextAccessor != null)
            {
                userId = httpContextAccessor.HttpContext?.GetUserId() ?? "";
            }
        }

        public async Task<SearchResponse> SearchDatasAsync(QuizFilterDto searchRequest)
        {
            // 1. Initialize the Base Query
            Query query = firestoreDb.Collection("quiz").WhereEqualTo("RecordStatus", RecordStatusConstant.Active);

            // 2. Filter by Category
            if (!string.IsNullOrEmpty(searchRequest.CategoryId))
            {
                query = query.WhereEqualTo("CategoryId", searchRequest.CategoryId);
            }

            // 3. Ordering (Firestore requires an index for this)
            if (searchRequest.OrderBy.Equals("createdTime", StringComparison.OrdinalIgnoreCase))
            {
                if (searchRequest.OrderDir.Equals("asc", StringComparison.OrdinalIgnoreCase))
                    query = query.OrderBy("CreatedTime");
                else
                    query = query.OrderByDescending("CreatedTime");
            }

            // 4. Firestore Search Limitation
            // Firestore does not support 'ILike' or '%search%'. 
            // You can only do 'StartsWith' using the range trick below.
            if (!string.IsNullOrWhiteSpace(searchRequest.Search))
            {
                string term = searchRequest.Search;
                query = query.WhereGreaterThanOrEqualTo("Title", term)
                            .WhereLessThanOrEqualTo("Title", term + "\uf8ff");
            }

            // 5. Execution & Manual Projection
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            
            // Total Items (Full count in Firestore is billed per document read or using Count() aggregation)
            var response = new SearchResponse
            {
                TotalItems = snapshot.Count,
                CurrentPage = searchRequest.CurrentPage,
                PageSize = searchRequest.PageSize
            };

            // Manual Pagination
            var skip = searchRequest.PageSize * searchRequest.CurrentPage;
            var rawItems = snapshot.Documents
                .Skip(skip)
                .Take(searchRequest.PageSize)
                .Select(doc => doc.ConvertTo<QuizModel>())
                .ToList();

            var listQuizDto = new List<QuizDto>();

            foreach (var quiz in rawItems)
            {
                var dto = mapper.Map<QuizDto>(quiz);

                // Note: Histories and Questions are separate collections.
                // To get counts, you must query those collections separately for each item
                // unless you store the counts directly on the Quiz document.
                if (dto.CategoryId != null)
                {
                    dto.Category = await categoryRepository.GetDataByIdAsync(dto.CategoryId);
                }

                if (dto.UserId != null)
                {
                    dto.User = await userRepository.GetSimpleUserDtoAsync(dto.UserId);
                }

                dto.QuestionCount = await GetCollectionCount("question", "QuizId", quiz.QuizId);
                dto.HistoriesCount = await GetCollectionCount("quizhistory", "QuizId", quiz.QuizId);
                
                // Check if taken by user
                dto.IsTakenByUser = await CheckIfTaken(quiz.QuizId, userId);

                listQuizDto.Add(dto);
            }

            response.Items = listQuizDto;
            return response;
        }

        private async Task<int> GetCollectionCount(string collection, string filterField, string filterValue)
        {
            Query query = firestoreDb.Collection(collection)
                .WhereEqualTo(filterField, filterValue)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active);

            AggregateQuerySnapshot snapshot = await query.Count().GetSnapshotAsync();
            return (int)(snapshot.Count ?? 0);
        }

        private async Task<bool> CheckIfTaken(string quizId, string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;

            Query query = firestoreDb.Collection("quizhistory")
                .WhereEqualTo("QuizId", quizId)
                .WhereEqualTo("UserId", userId)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents.Count > 0;
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

                    // save the answer
                    await firestoreDb.Collection("answer").AddAsync(answer);
                }

                questionOrder++;

                // save the question
                await firestoreDb.Collection("question").AddAsync(question);
            }

            // save the quiz
            await firestoreDb.Collection("quiz").AddAsync(quiz);
        }

        public async Task<QuizDto> GetDataByIdAsync(string id)
        {
            Query query = firestoreDb.Collection("quiz")
                .WhereEqualTo("QuizId", id)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count > 0)
            {
                var quiz = snapshot.Documents[0].ConvertTo<QuizModel>();
                var quizDto = mapper.Map<QuizDto>(quiz);
                quizDto.QuestionCount = await GetCollectionCount("question", "QuizId", quiz.QuizId);

                if (quizDto.CategoryId != null)
                {
                    quizDto.Category = await categoryRepository.GetDataByIdAsync(quizDto.CategoryId);
                }

                if (quizDto.UserId != null)
                {
                    quizDto.User = await userRepository.GetSimpleUserDtoAsync(quizDto.UserId);
                }

                if (userId != "")
                {
                    quizDto.IsTakenByUser = await CheckIfTaken(quiz.QuizId, userId);
                }

                return quizDto;
            }

            throw new KnownException(ErrorMessageConstant.DataNotFound);
        }

        public async Task<TakeQuizDto> TakeQuizByIdAsync(string quizId)
        {
            // 1. Fetch the Quiz Document
            Query query = firestoreDb.Collection("quiz")
                .WhereEqualTo("QuizId", quizId)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            var quiz = snapshot.Documents[0].ConvertTo<QuizModel>();

            // 2. Check if user has already taken the quiz (History check)
            // We query the 'history' collection for a match
            Query historyQuery = firestoreDb.Collection("quizhistory")
                .WhereEqualTo("QuizId", quizId)
                .WhereEqualTo("UserId", userId)
                .Limit(1);

            QuerySnapshot historySnap = await historyQuery.GetSnapshotAsync();
            if (historySnap.Documents.Count > 0)
            {
                throw new KnownException("Anda sudah mengerjakan kuis ini");
            }

            // 3. Fetch Active Questions for this Quiz
            Query questionsQuery = firestoreDb.Collection("question")
                .WhereEqualTo("QuizId", quizId)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .OrderBy("QuestionOrder");

            QuerySnapshot questionsSnap = await questionsQuery.GetSnapshotAsync();
            var questions = questionsSnap.Documents.Select(d => d.ConvertTo<QuestionModel>()).ToList();

            // 4. Fetch Answers for each Question
            // (Note: If you have many questions, consider storing answers as a nested array in the Question document)
            foreach (var question in questions)
            {
                Query answersQuery = firestoreDb.Collection("answer")
                    .WhereEqualTo("QuestionId", question.QuestionId)
                    .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                    .OrderBy("AnswerOrder");

                QuerySnapshot answersSnap = await answersQuery.GetSnapshotAsync();
                question.Answers = answersSnap.Documents.Select(d => d.ConvertTo<AnswerModel>()).ToList();
            }

            // 5. Map to DTO
            quiz.Questions = questions;
            TakeQuizDto quizDto = mapper.Map<TakeQuizDto>(quiz);
            quizDto.QuestionCount = quiz.Questions.Count;

            return quizDto;
        }
        
        public async Task<QuizDto> GetQuizWithQuestionsByIdAsync(string quizId)
        {
            // 1. Fetch the Quiz Document
            Query query = firestoreDb.Collection("quiz")
                .WhereEqualTo("QuizId", quizId)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            var quiz = snapshot.Documents[0].ConvertTo<QuizModel>();

            // 2. Check if user has already taken the quiz (History check)
            // We query the 'history' collection for a match
            Query historyQuery = firestoreDb.Collection("quizhistory")
                .WhereEqualTo("QuizId", quizId)
                .WhereEqualTo("UserId", userId)
                .Limit(1);

            QuerySnapshot historySnap = await historyQuery.GetSnapshotAsync();
            if (historySnap.Documents.Count > 0)
            {
                throw new KnownException("Anda sudah mengerjakan kuis ini");
            }

            // 3. Fetch Active Questions for this Quiz
            Query questionsQuery = firestoreDb.Collection("question")
                .WhereEqualTo("QuizId", quizId)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .OrderBy("QuestionOrder");

            QuerySnapshot questionsSnap = await questionsQuery.GetSnapshotAsync();
            var questions = questionsSnap.Documents.Select(d => d.ConvertTo<QuestionModel>()).ToList();

            // 4. Fetch Answers for each Question
            // (Note: If you have many questions, consider storing answers as a nested array in the Question document)
            foreach (var question in questions)
            {
                Query answersQuery = firestoreDb.Collection("answer")
                    .WhereEqualTo("QuestionId", question.QuestionId)
                    .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                    .OrderBy("AnswerOrder");

                QuerySnapshot answersSnap = await answersQuery.GetSnapshotAsync();
                question.Answers = answersSnap.Documents.Select(d => d.ConvertTo<AnswerModel>()).ToList();
            }

            // 5. Map to DTO
            quiz.Questions = questions;
            QuizDto quizDto = mapper.Map<QuizDto>(quiz);
            quizDto.QuestionCount = quiz.Questions.Count;

            if (quizDto.CategoryId != null)
            {
                quizDto.Category = await categoryRepository.GetDataByIdAsync(quizDto.CategoryId);
            }

            return quizDto;
        }

        public async Task<QuizDto> UpdateDataByIdAsync(string id, QuizDto quizDto)
        {
            // 1. Fetch the Quiz Document
            var quizCollection = firestoreDb.Collection("quiz");
            Query query = quizCollection
                .WhereEqualTo("QuizId", id)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            DocumentSnapshot quizDoc = snapshot.Documents[0];
            var editedQuiz = quizDoc.ConvertTo<QuizModel>();

            if (editedQuiz.UserId != userId)
            {
                throw new KnownException(ErrorMessageConstant.AccessNotAllowed);
            }

            // 2. Fetch Active Questions for this Quiz
            Query questionsQuery = firestoreDb.Collection("question")
                .WhereEqualTo("QuizId", id)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .OrderBy("QuestionOrder");

            QuerySnapshot questionsSnap = await questionsQuery.GetSnapshotAsync();
            var editedQuestions = questionsSnap.Documents.Select(d => d.ConvertTo<QuestionModel>()).ToList();

            // 3. Fetch Answers for each Question
            // (Note: If you have many questions, consider storing answers as a nested array in the Question document)
            foreach (var question in editedQuestions)
            {
                Query answersQuery = firestoreDb.Collection("answer")
                    .WhereEqualTo("QuestionId", question.QuestionId)
                    .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                    .OrderBy("AnswerOrder");

                QuerySnapshot answersSnap = await answersQuery.GetSnapshotAsync();
                question.Answers = answersSnap.Documents.Select(d => d.ConvertTo<AnswerModel>()).ToList();
            }

            // update the quiz
            editedQuiz.CategoryId = quizDto.CategoryId;
            editedQuiz.Title = quizDto.Title;
            editedQuiz.ImageUrl = quizDto.ImageUrl;
            editedQuiz.Time = quizDto.Time ?? 0; // Time is nullable in dto

            actionModelHelper.AssignUpdateModel(editedQuiz, userId);

            // start a batch
            WriteBatch batch = firestoreDb.StartBatch();
            
            // update the questions
            // questionDto is the updated question
            int questionOrder = 0;
            foreach (var questionDto in quizDto.Questions)
            {
                // find the question that is about to be updated
                var editedQuestion = editedQuestions.FirstOrDefault(q => q.QuestionId == questionDto.QuestionId);

                // if question is found in questionDto, update
                if (editedQuestion != null)
                {
                    editedQuestion.Text = questionDto.Text;
                    editedQuestion.ImageUrl = questionDto.ImageUrl;
                    editedQuestion.QuestionOrder = questionOrder;

                    actionModelHelper.AssignUpdateModel(editedQuestion, userId);

                    // update the answers
                    int answerOrder = 0;
                    foreach (var answerDto in questionDto.Answers)
                    {
                        // find the answer that is about to be updated   
                        AnswerModel? editedAnswer = editedQuestion.Answers.FirstOrDefault(a => a.AnswerId == answerDto.AnswerId);

                        // if the answer is found, update
                        if (editedAnswer != null)
                        {
                            editedAnswer.Text = answerDto.Text;
                            editedAnswer.ImageUrl = answerDto.ImageUrl;
                            editedAnswer.IsTrueAnswer = answerDto.IsTrueAnswer;
                            editedAnswer.AnswerOrder = answerOrder;

                            actionModelHelper.AssignUpdateModel(editedAnswer, userId);

                            // save the updated answer
                            var answerDocument = firestoreDb.Collection("answer");
                            Query answerQuery = answerDocument
                                .WhereEqualTo("AnswerId", answerDto.AnswerId)
                                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                                .Limit(1);
                            QuerySnapshot answerSnapshot = await answerQuery.GetSnapshotAsync();
                            DocumentSnapshot answerDoc = answerSnapshot.Documents[0];
                            batch.Set(answerDoc.Reference, editedAnswer); // Overwrites the doc with the updated model
                        }
                        // if the answer is not found
                        // answerDto is a new answer, add
                        else
                        {
                            AnswerModel newAnswer = mapper.Map<AnswerModel>(answerDto);
                            newAnswer.QuestionId = editedQuestion.QuestionId;
                            newAnswer.AnswerOrder = answerOrder;

                            actionModelHelper.AssignCreateModel(newAnswer, "Answer", userId);

                            // insert to question
                            // this action is prevented by concurrecy
                            // question.Answers.Add(newAnswer);

                            // add
                            await firestoreDb.Collection("answer").AddAsync(newAnswer);
                        }

                        answerOrder++;
                    }

                    // check old answers that dont exist in questionDto
                    foreach (var oldAnswer in editedQuestion.Answers)
                    {   
                        AnswerModel? editedAnswer = mapper.Map<AnswerModel>(questionDto.Answers.FirstOrDefault(a => a.AnswerId == oldAnswer.AnswerId));

                        // if answer is not found in questionDto
                        // delete
                        if (editedAnswer == null)
                        {
                            actionModelHelper.AssignDeleteModel(oldAnswer, userId);

                            var answerDocument = firestoreDb.Collection("answer");
                            Query answerQuery = answerDocument
                                .WhereEqualTo("AnswerId", oldAnswer.AnswerId)
                                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                                .Limit(1);
                            QuerySnapshot answerSnapshot = await answerQuery.GetSnapshotAsync();
                            DocumentSnapshot answerDoc = answerSnapshot.Documents[0];
                            batch.Set(answerDoc.Reference, oldAnswer); // Overwrites the doc with the updated model
                        }
                    }

                    // save the updated question
                    var questionDocument = firestoreDb.Collection("question");
                    Query questionQuery = questionDocument
                        .WhereEqualTo("QuestionId", questionDto.QuestionId)
                        .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                        .Limit(1);
                    QuerySnapshot questionSnapshot = await questionQuery.GetSnapshotAsync();
                    DocumentSnapshot questionDoc = questionSnapshot.Documents[0];
                    batch.Set(questionDoc.Reference, editedQuestion); // Overwrites the doc with the updated model
                }
                // if the question is not found
                // add
                else
                {
                    QuestionModel newQuestion = mapper.Map<QuestionModel>(questionDto);
                    newQuestion.QuizId = editedQuiz.QuizId;
                    newQuestion.QuestionOrder = questionOrder;

                    actionModelHelper.AssignCreateModel(newQuestion, "Question", userId);

                    // create the answers
                    int answerOrder = 0;
                    foreach (var answer in newQuestion.Answers)
                    {
                        answer.QuestionId = newQuestion.QuestionId;
                        answer.AnswerOrder = answerOrder;
                        actionModelHelper.AssignCreateModel(answer, "Answer", userId);

                        await firestoreDb.Collection("answer").AddAsync(answer);
                    }

                    // insert to quiz
                    // quiz.Questions.Add(newQuestion);

                    await firestoreDb.Collection("question").AddAsync(newQuestion);
                }

                questionOrder++;
            }

            // check old questions that dont exist in quizDto
            foreach (var oldQuestion in editedQuestions)
            {   
                QuestionModel? editedQuestion = mapper.Map<QuestionModel>(quizDto.Questions.FirstOrDefault(q => q.QuestionId == oldQuestion.QuestionId));

                // if old question is not found in quizDto
                // delete
                if (editedQuestion == null)
                {
                    actionModelHelper.AssignDeleteModel(oldQuestion, userId);
                    var questionDocument = firestoreDb.Collection("question");
                    Query questionQuery = questionDocument
                        .WhereEqualTo("QuestionId", oldQuestion.QuestionId)
                        .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                        .Limit(1);
                    QuerySnapshot questionSnapshot = await questionQuery.GetSnapshotAsync();
                    DocumentSnapshot questionDoc = questionSnapshot.Documents[0];

                    batch.Set(questionDoc.Reference, oldQuestion); // Overwrites the doc with the updated model
                }
            }

            batch.Set(quizDoc.Reference, editedQuiz); // Overwrites the doc with the updated model
            await batch.CommitAsync();

            return mapper.Map<QuizDto>(editedQuiz);
        }

        public async Task DeleteDataAsync(string id)
        {
            // Find the document
            var quizCollection = firestoreDb.Collection("quiz");
            Query query = quizCollection
                .WhereEqualTo("QuizId", id)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            DocumentSnapshot targetDoc = snapshot.Documents[0];
            QuizModel quiz = snapshot.Documents[0].ConvertTo<QuizModel>();

            // only creator of the quiz can remove the quiz
            if (quiz.UserId != userId)
            {
                throw new KnownException("Anda tidak diizinkan mengakses fitur ini");
            }

            WriteBatch batch = firestoreDb.StartBatch();

            actionModelHelper.AssignDeleteModel(quiz, userId);

            batch.Set(targetDoc.Reference, quiz); // Overwrites the doc with the updated model
            await batch.CommitAsync();
        }

        public async Task<QuizHistoryModel> CheckQuizAsync(CheckQuizDto checkQuizDto, string quizId)
        {
            Query query = firestoreDb.Collection("quiz")
                .WhereEqualTo("QuizId", quizId)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            var quiz = snapshot.Documents[0].ConvertTo<QuizModel>();

            Query historyQuery = firestoreDb.Collection("quizhistory")
                .WhereEqualTo("QuizId", quizId)
                .WhereEqualTo("UserId", userId)
                .Limit(1);

            QuerySnapshot historySnap = await historyQuery.GetSnapshotAsync();
            if (historySnap.Documents.Count > 0)
            {
                throw new KnownException("Anda sudah mengerjakan kuis ini");
            }

            // 2. Fetch Active Questions for this Quiz
            Query questionsQuery = firestoreDb.Collection("question")
                .WhereEqualTo("QuizId", quizId)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .OrderBy("QuestionOrder");

            QuerySnapshot questionsSnap = await questionsQuery.GetSnapshotAsync();
            var questions = questionsSnap.Documents.Select(d => d.ConvertTo<QuestionModel>()).ToList();

            // 3. Fetch Answers for each Question
            foreach (var question in questions)
            {
                Query answersQuery = firestoreDb.Collection("answer")
                    .WhereEqualTo("QuestionId", question.QuestionId)
                    .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                    .OrderBy("AnswerOrder");

                QuerySnapshot answersSnap = await answersQuery.GetSnapshotAsync();
                question.Answers = answersSnap.Documents.Select(d => d.ConvertTo<AnswerModel>()).ToList();
            }

            // 4. Map to DTO
            quiz.Questions = questions;

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
                CheckQuestionDto? checkQuestion = checkQuizDto.Questions.FirstOrDefault(x => x.QuestionOrder == question.QuestionOrder);

                // if the checked question is not found, the quiz is not valid
                if (checkQuestion == null)
                {
                    throw new KnownException(ErrorMessageConstant.DataNotFound);
                }
                else
                {
                    AnswerHistoryModel? selectedAnswer = question.Answers.FirstOrDefault(x => x.AnswerOrder == checkQuestion.SelectedAnswerOrder);

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

                    // save the QuestionHistory
                    await firestoreDb.Collection("questionhistory").AddAsync(question);
                }

                // assign id for answers
                foreach (var answer in question.Answers)
                {
                    answer.QuestionHistoryId = question.QuestionHistoryId;
                    actionModelHelper.AssignCreateModel(answer, "AnswerHistory", userId);

                    // save the AnswerHistory
                    await firestoreDb.Collection("answerhistory").AddAsync(answer);
                }

                question.Answers = question.Answers.OrderBy(x => x.AnswerOrder).ToList();
            }

            // quiz history metadata
            quizHistory.Questions = quizHistory.Questions.OrderBy(x => x.QuestionOrder).ToList();
            quizHistory.QuizVersion = checkQuizDto.QuizVersion;
            quizHistory.Duration = checkQuizDto.Duration;
            quizHistory.QuestionCount = checkQuizDto.QuestionCount;
            quizHistory.TrueAnswers = quizHistory.Questions.Count(x => x.IsAnswerTrue);
            quizHistory.WrongAnswers = quizHistory.QuestionCount - quizHistory.TrueAnswers;
            quizHistory.Score = (int)Math.Round((double)quizHistory.TrueAnswers / quizHistory.QuestionCount * 100);

            await firestoreDb.Collection("quizhistory").AddAsync(quizHistory);

            // send push notification for the creator
            // the notification is sent to all devices related to the creator
            // List<FcmTokenModel> fcmTokens = await dBContext.FcmToken
            //     .Where(x => x.UserId == quiz.UserId && x.RecordStatus == RecordStatusConstant.Active)
            //     .ToListAsync();
            // if there are fcm tokens (one or more)
            // if (fcmTokens.Any())
            // {
            //     UserModel? quizTaker = await dBContext.User.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            //     if (quizTaker != null)
            //     {
            //         var notificationQueue = new NotificationQueue
            //         {
            //             FcmTokens = fcmTokens.Select(x => x.Token).ToList(),
            //             Title = "Kuis Anda Dikerjakan",
            //             Body = $"{quizTaker.Name} telah mengerjakan kuis Anda: {quiz.Title}"
            //         };
            //     }
            // }

            return quizHistory;
        }

        public async Task<SearchResponse> GetHistoriesByQuizIdAsync(SearchRequestDto searchRequest, string quizId)
        {
            // 1. Reference your collection
            CollectionReference collection = firestoreDb.Collection("quizhistory");

            // 2. Initial Filtering
            // Note: RecordStatusConstant.Active must match the value stored in Firestore
            Query query = collection
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .WhereEqualTo("QuizId", quizId);

            // 3. Dynamic Ordering
            string orderBy = searchRequest.OrderBy; // e.g., "CreatedTime" (ensure camelCase/PascalCase match Firestore keys)
            bool isAscending = searchRequest.OrderDir.Equals("asc", StringComparison.OrdinalIgnoreCase);

            // Map DTO field names to Firestore document field names
            if (orderBy.Equals("createdTime", StringComparison.OrdinalIgnoreCase))
            {
                query = isAscending ? query.OrderBy("CreatedTime") : query.OrderByDescending("CreatedTime");
            }
            else if (orderBy.Equals("score", StringComparison.OrdinalIgnoreCase))
            {
                query = isAscending ? query.OrderBy("Score") : query.OrderByDescending("Score");
            }

            // 4. Get Total Count
            AggregateQuery countQuery = query.Count();
            AggregateQuerySnapshot countSnapshot = await countQuery.GetSnapshotAsync();
            long totalItems = countSnapshot.Count ?? 0;

            // 5. Pagination (Offset/Limit)
            int skip = searchRequest.PageSize * searchRequest.CurrentPage;
            int take = searchRequest.PageSize;
            
            // Warning: Firestore 'Offset' still bills for documents skipped. 
            // For large datasets, 'StartAfter' (cursor-based) is better.
            Query pagedQuery = query.Offset(skip).Limit(take);
            
            QuerySnapshot querySnapshot = await pagedQuery.GetSnapshotAsync();

            // 6. Map to Models
            var listQuizHistories = querySnapshot.Documents
                .Select(doc => doc.ConvertTo<QuizHistoryModel>())
                .ToList();

            // fetch user and quiz
            foreach (var history in listQuizHistories)
            {
                if (history.UserId != null)
                {
                    Query userQuery = firestoreDb.Collection("user")
                        .WhereEqualTo("UserId", history.UserId)
                        .Limit(1);

                    QuerySnapshot snapshot = await userQuery.GetSnapshotAsync();

                    if (snapshot.Documents.Count > 0)
                    {
                        UserModel user = snapshot.Documents[0].ConvertTo<UserModel>();
                        history.User = user;
                    }
                }

                if (history.QuizId != null)
                {
                    Query quizQuery = firestoreDb.Collection("quiz")
                        .WhereEqualTo("QuizId", history.QuizId)
                        .Limit(1);

                    QuerySnapshot snapshot = await quizQuery.GetSnapshotAsync();

                    if (snapshot.Documents.Count > 0)
                    {
                        QuizModel quiz = snapshot.Documents[0].ConvertTo<QuizModel>();
                        history.Quiz = quiz;
                    }
                }
            }

            return new SearchResponse
            {
                TotalItems = (int)totalItems,
                CurrentPage = searchRequest.CurrentPage,
                PageSize = searchRequest.PageSize,
                Items = mapper.Map<List<QuizHistoryDto>>(listQuizHistories)
            };
        }
    }
}