using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamAppColorsAndCurrencyIsCommon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCommon",
                table: "SteamCurrencies",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BackgroundColor",
                table: "SteamApps",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "SteamApps",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                table: "SteamApps",
                nullable: true);


            // Set the 'common' currencies
            migrationBuilder.Sql("UPDATE [SteamCurrencies] SET [IsCommon] = '1' WHERE [Name] IN ('NZD', 'AUD', 'GBP', 'EUR', 'CAD', 'USD')");

            // Set the Rust theme details
            migrationBuilder.Sql("UPDATE [SteamApps] SET [IconLargeUrl] = 'https://steamcdn-a.akamaihd.net/steam/apps/252490/header.jpg' WHERE [SteamId] = '252490'");
            migrationBuilder.Sql("UPDATE [SteamApps] SET [PrimaryColor] = '#CD412B' WHERE [SteamId] = '252490'");
            migrationBuilder.Sql("UPDATE [SteamApps] SET [SecondaryColor] = '#A4A6A7' WHERE [SteamId] = '252490'");
            migrationBuilder.Sql("UPDATE [SteamApps] SET [BackgroundColor] = '#333333' WHERE [SteamId] = '252490'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCommon",
                table: "SteamCurrencies");

            migrationBuilder.DropColumn(
                name: "BackgroundColor",
                table: "SteamApps");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "SteamApps");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                table: "SteamApps");
        }
    }
}
