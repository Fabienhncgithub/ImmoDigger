using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImmoDigger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompliantMultiSourceIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailMessageId",
                table: "PropertyListings",
                type: "character varying(998)",
                maxLength: 998,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSender",
                table: "PropertyListings",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailSubject",
                table: "PropertyListings",
                type: "character varying(998)",
                maxLength: 998,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Allowed",
                table: "ListingSources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CollectionMethod",
                table: "ListingSources",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ListingSources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RobotsCheckedAt",
                table: "ListingSources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsCheckedAt",
                table: "ListingSources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcessedEmailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailMessageId = table.Column<string>(type: "character varying(998)", maxLength: 998, nullable: false),
                    Subject = table.Column<string>(type: "character varying(998)", maxLength: 998, nullable: true),
                    Sender = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ListingsExtractedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedEmailMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedEmailMessages_EmailMessageId",
                table: "ProcessedEmailMessages",
                column: "EmailMessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedEmailMessages");

            migrationBuilder.DropColumn(
                name: "EmailMessageId",
                table: "PropertyListings");

            migrationBuilder.DropColumn(
                name: "EmailSender",
                table: "PropertyListings");

            migrationBuilder.DropColumn(
                name: "EmailSubject",
                table: "PropertyListings");

            migrationBuilder.DropColumn(
                name: "Allowed",
                table: "ListingSources");

            migrationBuilder.DropColumn(
                name: "CollectionMethod",
                table: "ListingSources");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ListingSources");

            migrationBuilder.DropColumn(
                name: "RobotsCheckedAt",
                table: "ListingSources");

            migrationBuilder.DropColumn(
                name: "TermsCheckedAt",
                table: "ListingSources");
        }
    }
}
