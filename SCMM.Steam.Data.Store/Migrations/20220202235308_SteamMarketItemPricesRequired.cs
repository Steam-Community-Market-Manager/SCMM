using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamMarketItemPricesRequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [SteamMarketItems] SET [SellPrices_Serialised] = '' WHERE [SellPrices_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "SellPrices_Serialised",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql("UPDATE [SteamMarketItems] SET [BuyPrices_Serialised] = '' WHERE [BuyPrices_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "BuyPrices_Serialised",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SellPrices_Serialised",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "BuyPrices_Serialised",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
