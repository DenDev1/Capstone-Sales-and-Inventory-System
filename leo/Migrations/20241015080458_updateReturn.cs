using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace leo.Migrations
{
    public partial class updateReturn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          

            migrationBuilder.AddColumn<DateTime>(
                name: "OrderDate",
                table: "Return",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

          
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.DropColumn(
                name: "OrderDate",
                table: "Return");

   
        }
    }
}
