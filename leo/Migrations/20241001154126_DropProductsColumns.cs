using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace leo.Migrations
{
    public partial class DropProductsColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.Supplier','U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Supplier_ProductsProductId' AND object_id = OBJECT_ID(N'dbo.Supplier'))
    BEGIN
        DROP INDEX [IX_Supplier_ProductsProductId] ON [Supplier];
    END

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Supplier') AND name = 'ProductsProductId')
    BEGIN
        ALTER TABLE [Supplier] DROP COLUMN [ProductsProductId];
    END

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Supplier') AND name = 'Products')
    BEGIN
        ALTER TABLE [Supplier] DROP COLUMN [Products];
    END
END
");
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
