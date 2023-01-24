using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAppsAddPublisherNameDiscordCommunitiesEconomyMedia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordCommunities_Serialised",
                table: "SteamApps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EconomyMedia_Serialised",
                table: "SteamApps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublisherName",
                table: "SteamApps",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordCommunities_Serialised",
                table: "SteamApps");

            migrationBuilder.DropColumn(
                name: "EconomyMedia_Serialised",
                table: "SteamApps");

            migrationBuilder.DropColumn(
                name: "PublisherName",
                table: "SteamApps");
        }
    }
}
