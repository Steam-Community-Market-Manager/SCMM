using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RenameAssetDescriptionCreatorAsCreatorProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_SteamProfiles_CreatorId",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameColumn(
                name: "CreatorId",
                table: "SteamAssetDescriptions",
                newName: "CreatorProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamAssetDescriptions_CreatorId",
                table: "SteamAssetDescriptions",
                newName: "IX_SteamAssetDescriptions_CreatorProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_SteamProfiles_CreatorProfileId",
                table: "SteamAssetDescriptions",
                column: "CreatorProfileId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_SteamProfiles_CreatorProfileId",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameColumn(
                name: "CreatorProfileId",
                table: "SteamAssetDescriptions",
                newName: "CreatorId");

            migrationBuilder.RenameIndex(
                name: "IX_SteamAssetDescriptions_CreatorProfileId",
                table: "SteamAssetDescriptions",
                newName: "IX_SteamAssetDescriptions_CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_SteamProfiles_CreatorId",
                table: "SteamAssetDescriptions",
                column: "CreatorId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
