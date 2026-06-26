using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveEase.Lessons.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorNameToLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[lessons].[Lessons]') AND name = N'InstructorName')
                        ALTER TABLE [lessons].[Lessons] ADD [InstructorName] nvarchar(max) NOT NULL DEFAULT N'';
                    """);
            }
            else
            {
                migrationBuilder.AddColumn<string>(
                    name: "InstructorName",
                    schema: "lessons",
                    table: "Lessons",
                    type: "TEXT",
                    nullable: false,
                    defaultValue: "");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstructorName",
                schema: "lessons",
                table: "Lessons");
        }
    }
}
