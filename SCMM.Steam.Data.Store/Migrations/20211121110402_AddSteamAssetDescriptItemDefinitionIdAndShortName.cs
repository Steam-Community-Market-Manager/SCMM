using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamAssetDescriptItemDefinitionIdAndShortName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ItemDefinitionId",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemShortName",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemDefinitionId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "ItemShortName",
                table: "SteamAssetDescriptions");
        }
    }
}
