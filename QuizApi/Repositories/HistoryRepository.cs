using System.Linq.Expressions;
using AutoMapper;
using Google.Cloud.Firestore;
using QuizApi.Constants;
using QuizApi.DTOs.QuizHistory;
using QuizApi.Exceptions;
using QuizApi.Models.QuizHistory;

namespace QuizApi.Repositories
{
    public class HistoryRepository
    {
        private readonly FirestoreDb firestoreDb;
        private readonly IMapper mapper;
        public HistoryRepository(
            [FromKeyedServices("quiz-db")] FirestoreDb firestoreDb,
            IMapper mapper
        )
        {
            this.firestoreDb = firestoreDb;
            this.mapper = mapper;
        }

        public async Task<QuizHistoryDto> GetDataByIdAsync(string id)
        {
            // 1. Fetch the Quiz Document
            Query query = firestoreDb.Collection("quizhistory")
                .WhereEqualTo("QuizHistoryId", id)
                .WhereEqualTo("RecordStatus", RecordStatusConstant.Active)
                .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
            {
                throw new KnownException(ErrorMessageConstant.DataNotFound);
            }

            var quizHistory = snapshot.Documents[0].ConvertTo<QuizHistoryModel>();

            // 3. Fetch Active Questions for this Quiz
            Query questionsQuery = firestoreDb.Collection("questionhistory")
                .WhereEqualTo("QuizHistoryId", id)
                .OrderBy("QuestionOrder");

            QuerySnapshot questionsSnap = await questionsQuery.GetSnapshotAsync();
            var questions = questionsSnap.Documents.Select(d => d.ConvertTo<QuestionHistoryModel>()).ToList();

            // 4. Fetch Answers for each Question
            // (Note: If you have many questions, consider storing answers as a nested array in the Question document)
            foreach (var question in questions)
            {
                Query answersQuery = firestoreDb.Collection("answerhistory")
                    .WhereEqualTo("QuestionHistoryId", question.QuestionHistoryId)
                    .OrderBy("AnswerOrder");

                QuerySnapshot answersSnap = await answersQuery.GetSnapshotAsync();
                question.Answers = answersSnap.Documents.Select(d => d.ConvertTo<AnswerHistoryModel>()).ToList();
            }

            // 5. Map to DTO
            quizHistory.Questions = questions;
            QuizHistoryDto quizHistoryDto = mapper.Map<QuizHistoryDto>(quizHistory);
            quizHistoryDto.QuestionCount = quizHistory.Questions.Count;

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