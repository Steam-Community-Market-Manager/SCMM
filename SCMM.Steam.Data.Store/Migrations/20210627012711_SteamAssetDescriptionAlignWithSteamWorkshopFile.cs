using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAssetDescriptionAlignWithSteamWorkshopFile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalSubscriptions",
                table: "SteamAssetDescriptions",
                newName: "LifetimeSubscriptions");

            migrationBuilder.AlterColumn<decimal>(
                name: "PreviewContentId",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LifetimeSubscriptions",
                table: "SteamAssetDescriptions",
                newName: "TotalSubscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "PreviewContentId",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldNullable: true);
        }
    }
}
