using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace leo.Migrations
{
    public partial class RemoveRememberMeColumnFromUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RememberMe",
                table: "Users");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RememberMe",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
