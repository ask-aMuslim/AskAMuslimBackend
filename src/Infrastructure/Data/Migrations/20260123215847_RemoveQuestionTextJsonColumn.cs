using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskAMuslimBackend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveQuestionTextJsonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuestionTextJson",
                table: "QATranslations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuestionTextJson",
                table: "QATranslations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
