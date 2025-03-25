using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propertease.Migrations
{
    /// <inheritdoc />
    public partial class propertyvisit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PropertyViewingRequests_BuyerId",
                table: "PropertyViewingRequests",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyViewingRequests_PropertyId",
                table: "PropertyViewingRequests",
                column: "PropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyViewingRequests_Users_BuyerId",
                table: "PropertyViewingRequests",
                column: "BuyerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyViewingRequests_properties_PropertyId",
                table: "PropertyViewingRequests",
                column: "PropertyId",
                principalTable: "properties",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropertyViewingRequests_Users_BuyerId",
                table: "PropertyViewingRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyViewingRequests_properties_PropertyId",
                table: "PropertyViewingRequests");

            migrationBuilder.DropIndex(
                name: "IX_PropertyViewingRequests_BuyerId",
                table: "PropertyViewingRequests");

            migrationBuilder.DropIndex(
                name: "IX_PropertyViewingRequests_PropertyId",
                table: "PropertyViewingRequests");
        }
    }
}
