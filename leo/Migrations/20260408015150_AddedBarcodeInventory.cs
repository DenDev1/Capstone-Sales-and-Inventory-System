using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace leo.Migrations
{
    public partial class AddedBarcodeInventory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Inventory_ProductId",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "FK_Return_Inventory_InventoriesProductId",
                table: "Return");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHistory_Supplier_SupplierId",
                table: "TransactionHistory");

            migrationBuilder.DropTable(
                name: "InventoryHistory");

            migrationBuilder.DropIndex(
                name: "IX_Return_InventoriesProductId",
                table: "Return");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "InventoriesProductId",
                table: "Return");

            migrationBuilder.RenameColumn(
                name: "ProfileName",
                table: "SupplierProfile",
                newName: "Supplier");

            migrationBuilder.RenameColumn(
                name: "ReferenceNo",
                table: "Order",
                newName: "Barcode");

            migrationBuilder.RenameColumn(
                name: "Suppliers",
                table: "Inventory",
                newName: "Barcode");

            migrationBuilder.RenameColumn(
                name: "InventoriesProductId",
                table: "Inventory",
                newName: "ProfileId");

            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "TransactionHistory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TransactionHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductsAndQuantities",
                table: "TransactionHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "TransactionHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "SupplierProfile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Supplier",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Supplier",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductsAndQuantities",
                table: "Supplier",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Supplier",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Return",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PartialPaymentAmount",
                table: "Order",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.Inventory','U') IS NOT NULL
BEGIN
    IF COLUMNPROPERTY(OBJECT_ID(N'dbo.Inventory'), N'ProductId', 'IsIdentity') = 0
    BEGIN
        PRINT 'Skipping Inventory.ProductId identity alteration. Manual adjustment required if the column is not already identity.';
    END
    IF COLUMNPROPERTY(OBJECT_ID(N'dbo.Inventory'), N'ProfileId', 'IsIdentity') = 1
    BEGIN
        PRINT 'Skipping Inventory.ProfileId identity removal. Manual adjustment required if necessary.';
    END
END
");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "Category",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Return_ProductId",
                table: "Return",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_ProfileId",
                table: "Inventory",
                column: "ProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_SupplierProfile_ProfileId",
                table: "Inventory",
                column: "ProfileId",
                principalTable: "SupplierProfile",
                principalColumn: "ProfileId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Inventory_ProductId",
                table: "Order",
                column: "ProductId",
                principalTable: "Inventory",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Return_Inventory_ProductId",
                table: "Return",
                column: "ProductId",
                principalTable: "Inventory",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionHistory_Supplier_SupplierId",
                table: "TransactionHistory",
                column: "SupplierId",
                principalTable: "Supplier",
                principalColumn: "SupplierId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_SupplierProfile_ProfileId",
                table: "Inventory");

            migrationBuilder.DropForeignKey(
                name: "FK_Order_Inventory_ProductId",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "FK_Return_Inventory_ProductId",
                table: "Return");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHistory_Supplier_SupplierId",
                table: "TransactionHistory");

            migrationBuilder.DropIndex(
                name: "IX_Return_ProductId",
                table: "Return");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_ProfileId",
                table: "Inventory");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TransactionHistory");

            migrationBuilder.DropColumn(
                name: "ProductsAndQuantities",
                table: "TransactionHistory");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "TransactionHistory");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "SupplierProfile");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "ProductsAndQuantities",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Supplier");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Return");

            migrationBuilder.DropColumn(
                name: "PartialPaymentAmount",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "Supplier",
                table: "SupplierProfile",
                newName: "ProfileName");

            migrationBuilder.RenameColumn(
                name: "Barcode",
                table: "Order",
                newName: "ReferenceNo");

            migrationBuilder.RenameColumn(
                name: "ProfileId",
                table: "Inventory",
                newName: "InventoriesProductId");

            migrationBuilder.RenameColumn(
                name: "Barcode",
                table: "Inventory",
                newName: "Suppliers");

            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "TransactionHistory",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "Supplier",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InventoriesProductId",
                table: "Return",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "Inventory",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "InventoriesProductId",
                table: "Inventory",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<string>(
                name: "CategoryName",
                table: "Category",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory",
                column: "InventoriesProductId");

            migrationBuilder.CreateTable(
                name: "InventoryHistory",
                columns: table => new
                {
                    InventoryHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoriesProductId = table.Column<int>(type: "int", nullable: true),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryHistory", x => x.InventoryHistoryId);
                    table.ForeignKey(
                        name: "FK_InventoryHistory_Inventory_InventoriesProductId",
                        column: x => x.InventoriesProductId,
                        principalTable: "Inventory",
                        principalColumn: "InventoriesProductId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Return_InventoriesProductId",
                table: "Return",
                column: "InventoriesProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryHistory_InventoriesProductId",
                table: "InventoryHistory",
                column: "InventoriesProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Inventory_ProductId",
                table: "Order",
                column: "ProductId",
                principalTable: "Inventory",
                principalColumn: "InventoriesProductId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Return_Inventory_InventoriesProductId",
                table: "Return",
                column: "InventoriesProductId",
                principalTable: "Inventory",
                principalColumn: "InventoriesProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionHistory_Supplier_SupplierId",
                table: "TransactionHistory",
                column: "SupplierId",
                principalTable: "Supplier",
                principalColumn: "SupplierId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
