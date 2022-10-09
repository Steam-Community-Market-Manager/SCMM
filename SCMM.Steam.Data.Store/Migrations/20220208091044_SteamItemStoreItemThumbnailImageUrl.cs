using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamItemStoreItemThumbnailImageUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamItemStores_FileData_ItemsThumbnailId",
                table: "SteamItemStores");

            migrationBuilder.DropIndex(
                name: "IX_SteamItemStores_ItemsThumbnailId",
                table: "SteamItemStores");

            migrationBuilder.DropColumn(
                name: "ItemsThumbnailId",
                table: "SteamItemStores");

            migrationBuilder.AddColumn<string>(
                name: "ItemsThumbnailUrl",
                table: "SteamItemStores",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemsThumbnailUrl",
                table: "SteamItemStores");

            migrationBuilder.AddColumn<Guid>(
                name: "ItemsThumbnailId",
                table: "SteamItemStores",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemStores_ItemsThumbnailId",
                table: "SteamItemStores",
                column: "ItemsThumbnailId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamItemStores_FileData_ItemsThumbnailId",
                table: "SteamItemStores",
                column: "ItemsThumbnailId",
                principalTable: "FileData",
                principalColumn: "Id");
        }
    }
}
