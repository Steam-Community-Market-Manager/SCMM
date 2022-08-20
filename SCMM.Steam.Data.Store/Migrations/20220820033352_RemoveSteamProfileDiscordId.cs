using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RemoveSteamProfileDiscordId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SteamProfiles_DiscordId",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "DiscordId",
                table: "SteamProfiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordId",
                table: "SteamProfiles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfiles_DiscordId",
                table: "SteamProfiles",
                column: "DiscordId",
                unique: true,
                filter: "[DiscordId] IS NOT NULL");
        }
    }
}
