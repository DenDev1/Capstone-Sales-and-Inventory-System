using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace leo.Migrations
{
    public partial class DropProductsColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the index associated with the ProductsProductId column
            migrationBuilder.DropIndex(
                name: "IX_Supplier_ProductsProductId", // Replace with your actual index name if different
                table: "Supplier");

            // Drop the Products column
            migrationBuilder.DropColumn(
                name: "Products",
                table: "Supplier");

            // Drop the ProductsProductId column
            migrationBuilder.DropColumn(
                name: "ProductsProductId",
                table: "Supplier");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // If needed, add back the Products column
            migrationBuilder.AddColumn<string>(
                name: "Products",
                table: "Supplier",
                nullable: true);

            // If needed, add back the ProductsProductId column
            migrationBuilder.AddColumn<int>(
                name: "ProductsProductId",
                table: "Supplier",
                nullable: false,
                defaultValue: 0);

            // Recreate the index if needed
            migrationBuilder.CreateIndex(
                name: "IX_Supplier_ProductsProductId",
                table: "Supplier",
                column: "ProductsProductId");
        }


    }
}
