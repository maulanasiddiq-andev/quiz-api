using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizApi.Migrations
{
    /// <inheritdoc />
    public partial class QuizModelsRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuestionsCount",
                table: "Quiz");

            migrationBuilder.AddColumn<string>(
                name: "QuizModelQuizId",
                table: "Question",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionModelQuestionId",
                table: "Answer",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Question_QuizModelQuizId",
                table: "Question",
                column: "QuizModelQuizId");

            migrationBuilder.CreateIndex(
                name: "IX_Answer_QuestionModelQuestionId",
                table: "Answer",
                column: "QuestionModelQuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Answer_Question_QuestionModelQuestionId",
                table: "Answer",
                column: "QuestionModelQuestionId",
                principalTable: "Question",
                principalColumn: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Question_Quiz_QuizModelQuizId",
                table: "Question",
                column: "QuizModelQuizId",
                principalTable: "Quiz",
                principalColumn: "QuizId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answer_Question_QuestionModelQuestionId",
                table: "Answer");

            migrationBuilder.DropForeignKey(
                name: "FK_Question_Quiz_QuizModelQuizId",
                table: "Question");

            migrationBuilder.DropIndex(
                name: "IX_Question_QuizModelQuizId",
                table: "Question");

            migrationBuilder.DropIndex(
                name: "IX_Answer_QuestionModelQuestionId",
                table: "Answer");

            migrationBuilder.DropColumn(
                name: "QuizModelQuizId",
                table: "Question");

            migrationBuilder.DropColumn(
                name: "QuestionModelQuestionId",
                table: "Answer");

            migrationBuilder.AddColumn<int>(
                name: "QuestionsCount",
                table: "Quiz",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
