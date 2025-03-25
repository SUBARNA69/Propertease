using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propertease.Migrations
{
    /// <inheritdoc />
    public partial class esrem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "BoostedProperties");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "BoostedProperties");

            migrationBuilder.DropColumn(
                name: "PaymentOrderId",
                table: "BoostedProperties");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "BoostedProperties");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionId",
                table: "BoostedProperties");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "PaymentOrderId",
                table: "BoostedProperties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "BoostedProperties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentTransactionId",
                table: "BoostedProperties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hours = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PropertyId",
                table: "PaymentTransactions",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_UserId",
                table: "PaymentTransactions",
                column: "UserId");
        }
    }
}
