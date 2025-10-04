using Microsoft.EntityFrameworkCore;
using QuizApi.Models.Auth;
using QuizApi.Models.Identity;
using QuizApi.Models.Quiz;
using QuizApi.Models.QuizHistory;

namespace QuizApi.Models
{
    public class QuizAppDBContext : DbContext
    {
        public QuizAppDBContext(DbContextOptions<QuizAppDBContext> options) : base(options) { }

        public DbSet<OtpModel> Otp { get; set; }
        public DbSet<UserTokenModel> UserToken { get; set; }
        public DbSet<UserModel> User { get; set; }
        public DbSet<CategoryModel> Category { get; set; }
        public DbSet<QuizModel> Quiz { get; set; }
        public DbSet<QuestionModel> Question { get; set; }
        public DbSet<AnswerModel> Answer { get; set; }
        public DbSet<QuizHistoryModel> QuizHistory { get; set; }
        public DbSet<QuestionHistoryModel> QuestionHistory { get; set; }
        public DbSet<AnswerHistoryModel> AnswerHistory { get; set; }
    }
}