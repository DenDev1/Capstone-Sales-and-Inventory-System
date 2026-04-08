using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace leo.Migrations
{
    public partial class RemoveSupplierProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, drop the foreign key if it exists
            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_SupplierProfile_ProfileId",
                table: "Inventory");

            // Drop the SupplierProfile table
            migrationBuilder.DropTable(
                name: "SupplierProfile");

            // Drop the ProfileId column from Inventory
            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "Inventory");

            // Add ImagePath column to Inventory
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Inventory",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the SupplierProfile table
            migrationBuilder.CreateTable(
                name: "SupplierProfile",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierProfile", x => x.ProfileId);
                });

            // Re-add the ProfileId column to Inventory
            migrationBuilder.AddColumn<int>(
                name: "ProfileId",
                table: "Inventory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Re-create the foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_SupplierProfile_ProfileId",
                table: "Inventory",
                column: "ProfileId",
                principalTable: "SupplierProfile",
                principalColumn: "ProfileId",
                onDelete: ReferentialAction.Cascade);

            // Remove ImagePath column
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Inventory");
        }
    }
}
