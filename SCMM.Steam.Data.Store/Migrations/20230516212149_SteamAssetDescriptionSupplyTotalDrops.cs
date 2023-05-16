using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    /// <inheritdoc />
    public partial class SteamAssetDescriptionSupplyTotalDrops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SupplyTotalDropsEstimated",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SupplyTotalDropsKnown",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplyTotalDropsEstimated",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "SupplyTotalDropsKnown",
                table: "SteamAssetDescriptions");
        }
    }
}
