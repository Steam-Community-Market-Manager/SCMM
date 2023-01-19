using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAppUniqueIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamWorkshopFiles_AppId",
                table: "SteamWorkshopFiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamWorkshopFiles_SteamId",
                table: "SteamWorkshopFiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamItemDefinitionsArchive_AppId",
                table: "SteamItemDefinitionsArchive");

            migrationBuilder.DropIndex(
                name: "IX_SteamItemDefinitionsArchive_Digest",
                table: "SteamItemDefinitionsArchive");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_AppId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_ClassId_ItemDefinitionId",
                table: "SteamAssetDescriptions");

            migrationBuilder.CreateIndex(
                name: "IX_SteamWorkshopFiles_AppId_SteamId",
                table: "SteamWorkshopFiles",
                columns: new[] { "AppId", "SteamId" },
                unique: true,
                filter: "[SteamId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemDefinitionsArchive_AppId_Digest",
                table: "SteamItemDefinitionsArchive",
                columns: new[] { "AppId", "Digest" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_AppId_ClassId_ItemDefinitionId",
                table: "SteamAssetDescriptions",
                columns: new[] { "AppId", "ClassId", "ItemDefinitionId" },
                unique: true,
                filter: "[ClassId] IS NOT NULL AND [ItemDefinitionId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamWorkshopFiles_AppId_SteamId",
                table: "SteamWorkshopFiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamItemDefinitionsArchive_AppId_Digest",
                table: "SteamItemDefinitionsArchive");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_AppId_ClassId_ItemDefinitionId",
                table: "SteamAssetDescriptions");

            migrationBuilder.CreateIndex(
                name: "IX_SteamWorkshopFiles_AppId",
                table: "SteamWorkshopFiles",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamWorkshopFiles_SteamId",
                table: "SteamWorkshopFiles",
                column: "SteamId",
                unique: true,
                filter: "[SteamId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemDefinitionsArchive_AppId",
                table: "SteamItemDefinitionsArchive",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemDefinitionsArchive_Digest",
                table: "SteamItemDefinitionsArchive",
                column: "Digest",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_AppId",
                table: "SteamAssetDescriptions",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_ClassId_ItemDefinitionId",
                table: "SteamAssetDescriptions",
                columns: new[] { "ClassId", "ItemDefinitionId" },
                unique: true,
                filter: "[ClassId] IS NOT NULL AND [ItemDefinitionId] IS NOT NULL");
        }
    }
}
