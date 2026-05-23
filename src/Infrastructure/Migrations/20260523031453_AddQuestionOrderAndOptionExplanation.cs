using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskAMuslimBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionOrderAndOptionExplanation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Questions' AND column_name = 'Order'
                    ) THEN
                        ALTER TABLE ""Questions"" ADD COLUMN ""Order"" integer NOT NULL DEFAULT 0;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Options' AND column_name = 'Explanation'
                    ) THEN
                        ALTER TABLE ""Options"" ADD COLUMN ""Explanation"" character varying(1000) NOT NULL DEFAULT '';
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "Options");
        }
    }
}
