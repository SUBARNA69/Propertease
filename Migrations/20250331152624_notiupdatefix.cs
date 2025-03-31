using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propertease.Migrations
{
    /// <inheritdoc />
    public partial class notiupdatefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_RecipientId",
                table: "Notifications");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_RecipientId",
                table: "Notifications",
                column: "RecipientId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_RecipientId",
                table: "Notifications");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_RecipientId",
                table: "Notifications",
                column: "RecipientId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
