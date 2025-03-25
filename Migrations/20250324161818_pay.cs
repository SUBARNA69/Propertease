using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propertease.Migrations
{
    /// <inheritdoc />
    public partial class pay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "BoostedProperties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "BoostedProperties",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "BoostedProperties",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "BoostedProperties");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "BoostedProperties");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "BoostedProperties");
        }
    }
}
