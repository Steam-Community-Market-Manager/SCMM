using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAssetDescriptionRenameImageToPreview : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_ImageId",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "SteamAssetDescriptions",
                newName: "PreviewUrl");

            migrationBuilder.RenameColumn(
                name: "ImageId",
                table: "SteamAssetDescriptions",
                newName: "PreviewId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamAssetDescriptions_ImageId",
                table: "SteamAssetDescriptions",
                newName: "IX_SteamAssetDescriptions_PreviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_PreviewId",
                table: "SteamAssetDescriptions",
                column: "PreviewId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_PreviewId",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameColumn(
                name: "PreviewUrl",
                table: "SteamAssetDescriptions",
                newName: "ImageUrl");

            migrationBuilder.RenameColumn(
                name: "PreviewId",
                table: "SteamAssetDescriptions",
                newName: "ImageId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamAssetDescriptions_PreviewId",
                table: "SteamAssetDescriptions",
                newName: "IX_SteamAssetDescriptions_ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_ImageId",
                table: "SteamAssetDescriptions",
                column: "ImageId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
