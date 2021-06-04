using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class DataTypeULongToLong : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "TotalSalesMin",
                table: "SteamStoreItems",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)");

            migrationBuilder.AlterColumn<long>(
                name: "TotalSalesMax",
                table: "SteamStoreItems",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TradableRestrictionDays",
                table: "SteamAssetDescriptions",
                type: "int",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TotalSubscriptions",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MarketableRestrictionDays",
                table: "SteamAssetDescriptions",
                type: "int",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CurrentSubscriptions",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSalesMin",
                table: "SteamStoreItems",
                type: "decimal(20,0)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSalesMax",
                table: "SteamStoreItems",
                type: "decimal(20,0)",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TradableRestrictionDays",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSubscriptions",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "MarketableRestrictionDays",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentSubscriptions",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
