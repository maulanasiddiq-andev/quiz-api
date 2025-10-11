using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizApi.Migrations
{
    /// <inheritdoc />
    public partial class EditQuizModelProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuestionsCount",
                table: "Quiz",
                newName: "QuestionCount");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedAnswerOrder",
                table: "QuestionHistory",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuestionCount",
                table: "Quiz",
                newName: "QuestionsCount");

            migrationBuilder.AlterColumn<int>(
                name: "SelectedAnswerOrder",
                table: "QuestionHistory",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
