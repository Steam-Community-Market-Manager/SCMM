using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SetSteamAssetDescriptionWorkshopFileAnalyticPrecision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "GlowRatio",
                table: "SteamAssetDescriptions",
                type: "decimal(20,20)",
                precision: 20,
                scale: 20,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CutoutRatio",
                table: "SteamAssetDescriptions",
                type: "decimal(20,20)",
                precision: 20,
                scale: 20,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "GlowRatio",
                table: "SteamAssetDescriptions",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,20)",
                oldPrecision: 20,
                oldScale: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CutoutRatio",
                table: "SteamAssetDescriptions",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,20)",
                oldPrecision: 20,
                oldScale: 20,
                oldNullable: true);
        }
    }
}
