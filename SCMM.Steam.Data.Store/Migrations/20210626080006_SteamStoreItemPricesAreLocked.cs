using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamStoreItemPricesAreLocked : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PricesAreLocked",
                table: "SteamStoreItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE [SteamStoreItems] SET [PricesAreLocked] = 1 WHERE [Price] IS NOT NULL
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricesAreLocked",
                table: "SteamStoreItems");
        }
    }
}
