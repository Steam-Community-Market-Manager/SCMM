using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RemoveSteamCurrencyAndLanguageDefaults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "SteamLanguages");

            migrationBuilder.DropColumn(
                name: "IsCommon",
                table: "SteamCurrencies");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "SteamCurrencies");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "SteamLanguages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCommon",
                table: "SteamCurrencies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "SteamCurrencies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
