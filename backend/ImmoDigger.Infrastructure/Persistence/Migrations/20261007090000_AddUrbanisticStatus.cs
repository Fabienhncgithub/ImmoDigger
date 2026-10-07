using ImmoDigger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImmoDigger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ImmoDiggerDbContext))]
[Migration("20261007090000_AddUrbanisticStatus")]
public partial class AddUrbanisticStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "UrbanisticStatus",
            table: "PropertyListings",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Unknown");

        migrationBuilder.CreateIndex(
            name: "IX_PropertyListings_UrbanisticStatus",
            table: "PropertyListings",
            column: "UrbanisticStatus");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_PropertyListings_UrbanisticStatus", table: "PropertyListings");
        migrationBuilder.DropColumn(name: "UrbanisticStatus", table: "PropertyListings");
    }
}
