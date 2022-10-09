using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamMarketItemLastCheckedThirdPartyPricesOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheckedPriceOn_Serialised",
                table: "SteamMarketItems");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCheckedThirdPartyPricesOn",
                table: "SteamMarketItems",
                type: "datetimeoffset",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheckedThirdPartyPricesOn",
                table: "SteamMarketItems");

            migrationBuilder.AddColumn<string>(
                name: "LastCheckedPriceOn_Serialised",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
