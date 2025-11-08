using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertiesForQuizHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "QuizHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Time",
                table: "QuizHistory",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "QuizHistory",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "QuizHistory");

            migrationBuilder.DropColumn(
                name: "Time",
                table: "QuizHistory");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "QuizHistory");
        }
    }
}
