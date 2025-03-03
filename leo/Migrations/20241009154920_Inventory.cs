using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace leo.Migrations
{
    public partial class Inventory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<string>(
            //    name: "UserRole",
            //    table: "AuditLogs",
            //    type: "nvarchar(max)",
            //    nullable: false,
            //    defaultValue: "");

            migrationBuilder.CreateTable(
                name: "InventoryHistory",
                columns: table => new
                {
                    InventoryHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    StockLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryHistory", x => x.InventoryHistoryId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryHistory");

            //migrationBuilder.DropColumn(
            //    name: "UserRole",
            //    table: "AuditLogs");
        }
    }
}
