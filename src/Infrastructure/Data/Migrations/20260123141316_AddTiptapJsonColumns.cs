using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskAMuslimBackend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTiptapJsonColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminNotes_Students_StudentId1",
                table: "AdminNotes");

            migrationBuilder.DropIndex(
                name: "IX_AdminNotes_StudentId1",
                table: "AdminNotes");

            migrationBuilder.DropColumn(
                name: "StudentId1",
                table: "AdminNotes");

            migrationBuilder.AddColumn<string>(
                name: "AnswerTextJson",
                table: "QATranslations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionTextJson",
                table: "QATranslations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Rating",
                table: "Preachers",
                type: "decimal(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "ContentJson",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranscriptJson",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerTextJson",
                table: "QATranslations");

            migrationBuilder.DropColumn(
                name: "QuestionTextJson",
                table: "QATranslations");

            migrationBuilder.DropColumn(
                name: "ContentJson",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "TranscriptJson",
                table: "Lessons");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rating",
                table: "Preachers",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(3,2)",
                oldPrecision: 3,
                oldScale: 2);

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId1",
                table: "AdminNotes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotes_StudentId1",
                table: "AdminNotes",
                column: "StudentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminNotes_Students_StudentId1",
                table: "AdminNotes",
                column: "StudentId1",
                principalTable: "Students",
                principalColumn: "Id");
        }
    }
}
