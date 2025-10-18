using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuizApi.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseModelForRoleModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "RoleModule",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "RoleModule",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "RoleModule",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedTime",
                table: "RoleModule",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RoleModule",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "RoleModule",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedTime",
                table: "RoleModule",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordStatus",
                table: "RoleModule",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "RoleModule",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "RoleModule");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "RoleModule");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "RoleModule");

            migrationBuilder.DropColumn(
                name: "DeletedTime",
                table: "RoleModule");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "RoleModule");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "RoleModule");

            migrationBuilder.DropColumn(
                name: "ModifiedTime",
                table: "RoleModule");

            migrationBuilder.DropColumn(
                name: "RecordStatus",
                table: "RoleModule");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "RoleModule");
        }
    }
}
