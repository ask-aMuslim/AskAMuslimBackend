using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskAMuslimBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetLessonDurationDefaultTo5Min : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    -- Set default value for Duration column to 5 minutes (300 seconds = '00:05:00')
                    ALTER TABLE ""Lessons"" ALTER COLUMN ""Duration"" SET DEFAULT '00:05:00';
                EXCEPTION WHEN OTHERS THEN
                    -- Ignore errors if column doesn't exist or already has default
                    NULL;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Lessons"" ALTER COLUMN ""Duration"" DROP DEFAULT;
            ");
        }
    }
}
