using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImmoDigger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyListingImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "PropertyListings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "PropertyListings");
        }
    }
}
