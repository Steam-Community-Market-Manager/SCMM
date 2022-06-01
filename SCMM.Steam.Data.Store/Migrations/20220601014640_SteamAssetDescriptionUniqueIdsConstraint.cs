using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAssetDescriptionUniqueIdsConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_ClassId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_ItemDefinitionId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_WorkshopFileId",
                table: "SteamAssetDescriptions");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_ClassId_ItemDefinitionId",
                table: "SteamAssetDescriptions",
                columns: new[] { "ClassId", "ItemDefinitionId" },
                unique: true,
                filter: "[ClassId] IS NOT NULL AND [ItemDefinitionId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_ClassId_ItemDefinitionId",
                table: "SteamAssetDescriptions");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_ClassId",
                table: "SteamAssetDescriptions",
                column: "ClassId",
                unique: true,
                filter: "[ClassId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_ItemDefinitionId",
                table: "SteamAssetDescriptions",
                column: "ItemDefinitionId",
                unique: true,
                filter: "[ItemDefinitionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_WorkshopFileId",
                table: "SteamAssetDescriptions",
                column: "WorkshopFileId",
                unique: true,
                filter: "[WorkshopFileId] IS NOT NULL");
        }
    }
}
