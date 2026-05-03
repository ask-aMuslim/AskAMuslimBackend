using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskAMuslimBackend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HijriYear = table.Column<int>(type: "int", nullable: false),
                    HijriMonth = table.Column<int>(type: "int", nullable: false),
                    NewMuslims = table.Column<int>(type: "int", nullable: false),
                    Dialogues = table.Column<int>(type: "int", nullable: false),
                    CoursesCompleted = table.Column<int>(type: "int", nullable: false),
                    ActiveVolunteers = table.Column<int>(type: "int", nullable: false),
                    SocialMediaReach = table.Column<int>(type: "int", nullable: false),
                    EventsHeld = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_HijriYear_HijriMonth",
                table: "Achievements",
                columns: new[] { "HijriYear", "HijriMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Achievements");
        }
    }
}
