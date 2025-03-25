using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propertease.Migrations
{
    /// <inheritdoc />
    public partial class paysewa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "BoostedProperties");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "BoostedProperties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "BoostedProperties");

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "BoostedProperties",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
