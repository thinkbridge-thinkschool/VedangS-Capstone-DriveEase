using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveEase.Enrollments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxRetryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                // Conditional DDL: safe to re-run if columns were partially added
                migrationBuilder.Sql("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[enrollments].[OutboxMessages]') AND name = N'DeadLettered')
                        ALTER TABLE [enrollments].[OutboxMessages] ADD [DeadLettered] BIT NOT NULL DEFAULT 0;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[enrollments].[OutboxMessages]') AND name = N'RetryCount')
                        ALTER TABLE [enrollments].[OutboxMessages] ADD [RetryCount] INT NOT NULL DEFAULT 0;
                    """);
            }
            else
            {
                migrationBuilder.AddColumn<bool>(
                    name: "DeadLettered",
                    schema: "enrollments",
                    table: "OutboxMessages",
                    type: "INTEGER",
                    nullable: false,
                    defaultValue: false);

                migrationBuilder.AddColumn<int>(
                    name: "RetryCount",
                    schema: "enrollments",
                    table: "OutboxMessages",
                    type: "INTEGER",
                    nullable: false,
                    defaultValue: 0);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeadLettered",
                schema: "enrollments",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "enrollments",
                table: "OutboxMessages");
        }
    }
}
