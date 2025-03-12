using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Final.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OriginalPrice",
                table: "Products",
                newName: "DiscountPrice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiscountPrice",
                table: "Products",
                newName: "OriginalPrice");
        }
    }
}
