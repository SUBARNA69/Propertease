using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propertease.Migrations
{
    /// <inheritdoc />
    public partial class payesewa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoostedProperties_properties_PropertyId",
                table: "BoostedProperties");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "BoostedProperties",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "BoostedProperties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_BoostedProperties_properties_PropertyId",
                table: "BoostedProperties",
                column: "PropertyId",
                principalTable: "properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoostedProperties_properties_PropertyId",
                table: "BoostedProperties");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "BoostedProperties",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "BoostedProperties",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "Pending");

            migrationBuilder.AddForeignKey(
                name: "FK_BoostedProperties_properties_PropertyId",
                table: "BoostedProperties",
                column: "PropertyId",
                principalTable: "properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
