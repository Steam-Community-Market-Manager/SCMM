using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RemoveUnusedFlagsAndCleanupStoreItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Flags",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "SteamStoreItems",
                newName: "IsAvailable");

            migrationBuilder.AlterColumn<long>(
                name: "TotalSalesMin",
                table: "SteamStoreItems",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "Price",
                table: "SteamStoreItems",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsAvailable",
                table: "SteamStoreItems",
                newName: "IsActive");

            migrationBuilder.AlterColumn<long>(
                name: "TotalSalesMin",
                table: "SteamStoreItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Price",
                table: "SteamStoreItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Flags",
                table: "SteamStoreItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "Flags",
                table: "SteamMarketItems",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "Flags",
                table: "SteamAssetWorkshopFiles",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }
    }
}
