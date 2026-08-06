using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImmoDigger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListingSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PollingIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    LastSuccessfulRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailedRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AskingPrice = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    CurrentBid = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    EstimatedFinalPrice = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    SaleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PropertyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BedroomCount = table.Column<int>(type: "integer", nullable: true),
                    BathroomCount = table.Column<int>(type: "integer", nullable: true),
                    OfficialUnitCount = table.Column<int>(type: "integer", nullable: true),
                    ObservedUnitCount = table.Column<int>(type: "integer", nullable: true),
                    LivingArea = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    LandArea = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    PebRating = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    PebConsumption = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ElectricalInstallationCompliant = table.Column<bool>(type: "boolean", nullable: true),
                    IsOccupied = table.Column<bool>(type: "boolean", nullable: true),
                    HasGarage = table.Column<bool>(type: "boolean", nullable: true),
                    HasTerrace = table.Column<bool>(type: "boolean", nullable: true),
                    HasGarden = table.Column<bool>(type: "boolean", nullable: true),
                    CadastralIncome = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    AuctionStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuctionEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RawContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OpportunityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    EstimatedGrossYield = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true),
                    EstimatedRenovationCost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RiskSummary = table.Column<string>(type: "text", nullable: true),
                    EstimatedMonthlyRentPerUnit = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    EstimatedAcquisitionCosts = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    EstimatedRenovationBudget = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    PersonalNotes = table.Column<string>(type: "text", nullable: true),
                    IsReviewed = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyListings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MaximumPrice = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    MinimumGrossYield = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true),
                    MinimumUnitCount = table.Column<int>(type: "integer", nullable: true),
                    MinimumLivingArea = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    RequireGarage = table.Column<bool>(type: "boolean", nullable: false),
                    IncludePublicSales = table.Column<bool>(type: "boolean", nullable: false),
                    PostalCodes = table.Column<string[]>(type: "text[]", nullable: false),
                    PropertyTypes = table.Column<string[]>(type: "text[]", nullable: false),
                    MinimumOpportunityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListingPriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingPriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingPriceHistories_PropertyListings_PropertyListingId",
                        column: x => x.PropertyListingId,
                        principalTable: "PropertyListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationHistories_PropertyListings_PropertyListingId",
                        column: x => x.PropertyListingId,
                        principalTable: "PropertyListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingPriceHistories_PropertyListingId_RecordedAt",
                table: "ListingPriceHistories",
                columns: new[] { "PropertyListingId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ListingSources_Name",
                table: "ListingSources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistories_PropertyListingId_Channel",
                table: "NotificationHistories",
                columns: new[] { "PropertyListingId", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_City",
                table: "PropertyListings",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_FirstSeenAt",
                table: "PropertyListings",
                column: "FirstSeenAt");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_IsActive",
                table: "PropertyListings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_OpportunityScore",
                table: "PropertyListings",
                column: "OpportunityScore");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_PostalCode",
                table: "PropertyListings",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_Source_ExternalId",
                table: "PropertyListings",
                columns: new[] { "Source", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_Url",
                table: "PropertyListings",
                column: "Url");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingPriceHistories");

            migrationBuilder.DropTable(
                name: "ListingSources");

            migrationBuilder.DropTable(
                name: "NotificationHistories");

            migrationBuilder.DropTable(
                name: "SearchProfiles");

            migrationBuilder.DropTable(
                name: "PropertyListings");
        }
    }
}
