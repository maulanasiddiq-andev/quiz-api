using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuizHistory",
                columns: table => new
                {
                    QuizHistoryId = table.Column<string>(type: "text", nullable: false),
                    QuizId = table.Column<string>(type: "text", nullable: false),
                    QuizVersion = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    QuestionCount = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    TrueAnswers = table.Column<int>(type: "integer", nullable: false),
                    WrongAnswers = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    RecordStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizHistory", x => x.QuizHistoryId);
                    table.ForeignKey(
                        name: "FK_QuizHistory_Quiz_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quiz",
                        principalColumn: "QuizId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuizHistory_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionHistory",
                columns: table => new
                {
                    QuestionHistoryId = table.Column<string>(type: "text", nullable: false),
                    QuizHistoryId = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    QuestionOrder = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    SelectedAnswerOrder = table.Column<int>(type: "integer", nullable: false),
                    IsAnswerTrue = table.Column<bool>(type: "boolean", nullable: false),
                    QuizHistoryModelQuizHistoryId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionHistory", x => x.QuestionHistoryId);
                    table.ForeignKey(
                        name: "FK_QuestionHistory_QuizHistory_QuizHistoryModelQuizHistoryId",
                        column: x => x.QuizHistoryModelQuizHistoryId,
                        principalTable: "QuizHistory",
                        principalColumn: "QuizHistoryId");
                });

            migrationBuilder.CreateTable(
                name: "AnswerHistory",
                columns: table => new
                {
                    AnswerHistoryId = table.Column<string>(type: "text", nullable: false),
                    QuestionHistoryId = table.Column<string>(type: "text", nullable: false),
                    AnswerOrder = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    IsTrueAnswer = table.Column<bool>(type: "boolean", nullable: false),
                    QuestionHistoryModelQuestionHistoryId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerHistory", x => x.AnswerHistoryId);
                    table.ForeignKey(
                        name: "FK_AnswerHistory_QuestionHistory_QuestionHistoryModelQuestionH~",
                        column: x => x.QuestionHistoryModelQuestionHistoryId,
                        principalTable: "QuestionHistory",
                        principalColumn: "QuestionHistoryId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerHistory_QuestionHistoryModelQuestionHistoryId",
                table: "AnswerHistory",
                column: "QuestionHistoryModelQuestionHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionHistory_QuizHistoryModelQuizHistoryId",
                table: "QuestionHistory",
                column: "QuizHistoryModelQuizHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizHistory_QuizId",
                table: "QuizHistory",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizHistory_UserId",
                table: "QuizHistory",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnswerHistory");

            migrationBuilder.DropTable(
                name: "QuestionHistory");

            migrationBuilder.DropTable(
                name: "QuizHistory");
        }
    }
}
