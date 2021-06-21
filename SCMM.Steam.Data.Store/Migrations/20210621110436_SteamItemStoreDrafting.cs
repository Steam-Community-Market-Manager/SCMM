using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamItemStoreDrafting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalSalesGraph_Serialised",
                table: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "IndexGraph_Serialised",
                table: "SteamStoreItemItemStore");

            migrationBuilder.RenameColumn(
                name: "Index",
                table: "SteamStoreItemItemStore",
                newName: "TopSellerIndex");

            migrationBuilder.AlterColumn<int>(
                name: "TopSellerIndex",
                table: "SteamStoreItemItemStore",
                type: "int",
                nullable: true,
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "SteamStoreItemItemStore",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "SteamItemStores",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "SteamStoreItemItemStore");

            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "SteamItemStores");

            migrationBuilder.RenameColumn(
                name: "TopSellerIndex",
                table: "SteamStoreItemItemStore",
                newName: "Index");

            migrationBuilder.AlterColumn<int>(
                name: "Index",
                table: "SteamStoreItemItemStore",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "TotalSalesGraph_Serialised",
                table: "SteamStoreItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndexGraph_Serialised",
                table: "SteamStoreItemItemStore",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
