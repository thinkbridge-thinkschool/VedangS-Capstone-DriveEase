using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveEase.Lessons.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentNameToLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite: StudentName is already in InitialCreate (migration was regenerated in-place).
            // SQL Server: the table was created from the old InitialCreate which lacked StudentName.
            if (migrationBuilder.ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[lessons].[Lessons]') AND name = N'StudentName')
                        ALTER TABLE [lessons].[Lessons] ADD [StudentName] nvarchar(max) NOT NULL DEFAULT N'Unknown';
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("""
                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[lessons].[Lessons]') AND name = N'StudentName')
                        ALTER TABLE [lessons].[Lessons] DROP COLUMN [StudentName];
                    """);
            }
        }
    }
}
