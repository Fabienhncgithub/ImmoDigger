using ImmoDigger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImmoDigger.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ImmoDiggerDbContext))]
[Migration("20260819230000_AddOfficialDocumentEvidence")]
public partial class AddOfficialDocumentEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OfficialDocumentsJson",
            table: "PropertyListings",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "OfficialUnitCountSourceName",
            table: "PropertyListings",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OfficialUnitCountSourceUrl",
            table: "PropertyListings",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "OfficialDocumentsJson", table: "PropertyListings");
        migrationBuilder.DropColumn(name: "OfficialUnitCountSourceName", table: "PropertyListings");
        migrationBuilder.DropColumn(name: "OfficialUnitCountSourceUrl", table: "PropertyListings");
    }
}
