using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propertease.Migrations
{
    /// <inheritdoc />
    public partial class payfinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransactionCode",
                table: "BoostedProperties",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionCode",
                table: "BoostedProperties");
        }
    }
}
