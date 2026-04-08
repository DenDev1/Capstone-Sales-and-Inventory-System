using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace leo.Migrations
{
    public partial class AddUserIdForeignKeyToAuditLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Both index and FK already exist from AuditLogs setup in previous migrations
            // Skip duplicate creation to avoid errors
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Skip to preserve schema integrity
        }
    }
}
