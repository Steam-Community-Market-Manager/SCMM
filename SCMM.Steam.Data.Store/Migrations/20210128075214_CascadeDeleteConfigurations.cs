using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class CascadeDeleteConfigurations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordConfiguration_DiscordGuilds_DiscordGuildId",
                table: "DiscordConfiguration");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfileConfiguration_SteamProfiles_SteamProfileId",
                table: "SteamProfileConfiguration");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordConfiguration_DiscordGuilds_DiscordGuildId",
                table: "DiscordConfiguration",
                column: "DiscordGuildId",
                principalTable: "DiscordGuilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfileConfiguration_SteamProfiles_SteamProfileId",
                table: "SteamProfileConfiguration",
                column: "SteamProfileId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordConfiguration_DiscordGuilds_DiscordGuildId",
                table: "DiscordConfiguration");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfileConfiguration_SteamProfiles_SteamProfileId",
                table: "SteamProfileConfiguration");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordConfiguration_DiscordGuilds_DiscordGuildId",
                table: "DiscordConfiguration",
                column: "DiscordGuildId",
                principalTable: "DiscordGuilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfileConfiguration_SteamProfiles_SteamProfileId",
                table: "SteamProfileConfiguration",
                column: "SteamProfileId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
