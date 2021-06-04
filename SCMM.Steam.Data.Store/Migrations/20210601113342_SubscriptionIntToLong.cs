using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SubscriptionIntToLong : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSalesMin",
                table: "SteamStoreItems",
                type: "decimal(20,0)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSalesMax",
                table: "SteamStoreItems",
                type: "decimal(20,0)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSubscriptions",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentSubscriptions",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TotalSalesMin",
                table: "SteamStoreItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)");

            migrationBuilder.AlterColumn<int>(
                name: "TotalSalesMax",
                table: "SteamStoreItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TotalSubscriptions",
                table: "SteamAssetDescriptions",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CurrentSubscriptions",
                table: "SteamAssetDescriptions",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldNullable: true);
        }
    }
}
