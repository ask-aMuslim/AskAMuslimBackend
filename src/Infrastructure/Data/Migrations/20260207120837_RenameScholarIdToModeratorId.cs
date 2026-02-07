using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskAMuslimBackend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameScholarIdToModeratorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InquiryRequests_Users_ScholarId",
                table: "InquiryRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_MeetingRequests_Users_ScholarId",
                table: "MeetingRequests");

            migrationBuilder.RenameColumn(
                name: "ScholarId",
                table: "MeetingRequests",
                newName: "ModeratorId");

            migrationBuilder.RenameIndex(
                name: "IX_MeetingRequests_ScholarId_ScheduledAt",
                table: "MeetingRequests",
                newName: "IX_MeetingRequests_ModeratorId_ScheduledAt");

            migrationBuilder.RenameColumn(
                name: "ScholarId",
                table: "InquiryRequests",
                newName: "ModeratorId");

            migrationBuilder.RenameIndex(
                name: "IX_InquiryRequests_ScholarId_Created",
                table: "InquiryRequests",
                newName: "IX_InquiryRequests_ModeratorId_Created");

            migrationBuilder.AddForeignKey(
                name: "FK_InquiryRequests_Users_ModeratorId",
                table: "InquiryRequests",
                column: "ModeratorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingRequests_Users_ModeratorId",
                table: "MeetingRequests",
                column: "ModeratorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InquiryRequests_Users_ModeratorId",
                table: "InquiryRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_MeetingRequests_Users_ModeratorId",
                table: "MeetingRequests");

            migrationBuilder.RenameColumn(
                name: "ModeratorId",
                table: "MeetingRequests",
                newName: "ScholarId");

            migrationBuilder.RenameIndex(
                name: "IX_MeetingRequests_ModeratorId_ScheduledAt",
                table: "MeetingRequests",
                newName: "IX_MeetingRequests_ScholarId_ScheduledAt");

            migrationBuilder.RenameColumn(
                name: "ModeratorId",
                table: "InquiryRequests",
                newName: "ScholarId");

            migrationBuilder.RenameIndex(
                name: "IX_InquiryRequests_ModeratorId_Created",
                table: "InquiryRequests",
                newName: "IX_InquiryRequests_ScholarId_Created");

            migrationBuilder.AddForeignKey(
                name: "FK_InquiryRequests_Users_ScholarId",
                table: "InquiryRequests",
                column: "ScholarId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingRequests_Users_ScholarId",
                table: "MeetingRequests",
                column: "ScholarId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
