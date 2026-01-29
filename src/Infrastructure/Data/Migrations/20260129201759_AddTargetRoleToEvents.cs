using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskAMuslimBackend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetRoleToEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetRole",
                table: "Events",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetRole",
                table: "Events");
        }
    }
}
