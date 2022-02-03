using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAssetDescriptionsNullableIdsAndMarketItemManipulationFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_ClassId",
                table: "SteamAssetDescriptions");

            migrationBuilder.AddColumn<bool>(
                name: "IsBeingManipulated",
                table: "SteamMarketItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "ClassId",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)");

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

        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "IsBeingManipulated",
                table: "SteamMarketItems");

            migrationBuilder.AlterColumn<decimal>(
                name: "ClassId",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_ClassId",
                table: "SteamAssetDescriptions",
                column: "ClassId",
                unique: true);
        }
    }
}
