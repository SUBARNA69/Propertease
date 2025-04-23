using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Propertease.Migrations
{
    /// <inheritdoc />
    public partial class propertcommentremoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropertyComments_Users_UserId",
                table: "PropertyComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyComments_properties_PropertyId",
                table: "PropertyComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyViews_Users_UserId",
                table: "PropertyViews");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyViews_properties_PropertyId",
                table: "PropertyViews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PropertyViews",
                table: "PropertyViews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PropertyComments",
                table: "PropertyComments");

            migrationBuilder.RenameTable(
                name: "PropertyViews",
                newName: "PropertyView");

            migrationBuilder.RenameTable(
                name: "PropertyComments",
                newName: "PropertyComment");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyViews_UserId",
                table: "PropertyView",
                newName: "IX_PropertyView_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyViews_PropertyId",
                table: "PropertyView",
                newName: "IX_PropertyView_PropertyId");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyComments_UserId",
                table: "PropertyComment",
                newName: "IX_PropertyComment_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyComments_PropertyId",
                table: "PropertyComment",
                newName: "IX_PropertyComment_PropertyId");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "bit",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PropertyView",
                table: "PropertyView",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PropertyComment",
                table: "PropertyComment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyComment_Users_UserId",
                table: "PropertyComment",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyComment_properties_PropertyId",
                table: "PropertyComment",
                column: "PropertyId",
                principalTable: "properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyView_Users_UserId",
                table: "PropertyView",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyView_properties_PropertyId",
                table: "PropertyView",
                column: "PropertyId",
                principalTable: "properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropertyComment_Users_UserId",
                table: "PropertyComment");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyComment_properties_PropertyId",
                table: "PropertyComment");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyView_Users_UserId",
                table: "PropertyView");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyView_properties_PropertyId",
                table: "PropertyView");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PropertyView",
                table: "PropertyView");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PropertyComment",
                table: "PropertyComment");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "PropertyView",
                newName: "PropertyViews");

            migrationBuilder.RenameTable(
                name: "PropertyComment",
                newName: "PropertyComments");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyView_UserId",
                table: "PropertyViews",
                newName: "IX_PropertyViews_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyView_PropertyId",
                table: "PropertyViews",
                newName: "IX_PropertyViews_PropertyId");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyComment_UserId",
                table: "PropertyComments",
                newName: "IX_PropertyComments_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyComment_PropertyId",
                table: "PropertyComments",
                newName: "IX_PropertyComments_PropertyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PropertyViews",
                table: "PropertyViews",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PropertyComments",
                table: "PropertyComments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyComments_Users_UserId",
                table: "PropertyComments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyComments_properties_PropertyId",
                table: "PropertyComments",
                column: "PropertyId",
                principalTable: "properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyViews_Users_UserId",
                table: "PropertyViews",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyViews_properties_PropertyId",
                table: "PropertyViews",
                column: "PropertyId",
                principalTable: "properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
