using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propertease.Migrations
{
    /// <inheritdoc />
    public partial class userrating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRatings");

            migrationBuilder.CreateTable(
                name: "BuyerRatings",
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
                    table.PrimaryKey("PK_BuyerRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuyerRatings_PropertyViewingRequests_ViewingRequestId",
                        column: x => x.ViewingRequestId,
                        principalTable: "PropertyViewingRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BuyerRatings_Users_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BuyerRatings_Users_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BuyerRatings_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "properties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuyerRatings_BuyerId",
                table: "BuyerRatings",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_BuyerRatings_PropertyId",
                table: "BuyerRatings",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_BuyerRatings_SellerId",
                table: "BuyerRatings",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_BuyerRatings_ViewingRequestId",
                table: "BuyerRatings",
                column: "ViewingRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuyerRatings");

            migrationBuilder.CreateTable(
                name: "UserRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RatedUserId = table.Column<int>(type: "int", nullable: true),
                    RaterUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRatings_Users_RatedUserId",
                        column: x => x.RatedUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRatings_Users_RaterUserId",
                        column: x => x.RaterUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRatings_RatedUserId",
                table: "UserRatings",
                column: "RatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRatings_RaterUserId",
                table: "UserRatings",
                column: "RaterUserId");
        }
    }
}
