using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    /// <inheritdoc />
    public partial class AddSteamAssetDescriptionLootCrateDrop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsSpecialDrop",
                table: "SteamAssetDescriptions",
                newName: "IsPublisherDrop");

            migrationBuilder.AddColumn<bool>(
                name: "IsLootCrateDrop",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLootCrateDrop",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameColumn(
                name: "IsPublisherDrop",
                table: "SteamAssetDescriptions",
                newName: "IsSpecialDrop");
        }
    }
}
