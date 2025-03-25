using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propertease.Migrations
{
    /// <inheritdoc />
    public partial class rating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SellerRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    BuyerId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Review = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ViewingRequestId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerRatings_PropertyViewingRequests_ViewingRequestId",
                        column: x => x.ViewingRequestId,
                        principalTable: "PropertyViewingRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SellerRatings_Users_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SellerRatings_Users_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SellerRatings_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "properties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_BuyerId",
                table: "SellerRatings",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_PropertyId",
                table: "SellerRatings",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_SellerId",
                table: "SellerRatings",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_ViewingRequestId",
                table: "SellerRatings",
                column: "ViewingRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerRatings");
        }
    }
}
