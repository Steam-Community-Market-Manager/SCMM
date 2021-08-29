using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RemoveStoreItemPricesAreLocked : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricesAreLocked",
                table: "SteamStoreItems");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PricesAreLocked",
                table: "SteamStoreItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
