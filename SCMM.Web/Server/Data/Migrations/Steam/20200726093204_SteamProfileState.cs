using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamProfileState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "SteamProfiles",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LanguageId",
                table: "SteamProfiles",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfiles_CurrencyId",
                table: "SteamProfiles",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfiles_LanguageId",
                table: "SteamProfiles",
                column: "LanguageId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfiles_SteamCurrencies_CurrencyId",
                table: "SteamProfiles",
                column: "CurrencyId",
                principalTable: "SteamCurrencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfiles_SteamLanguages_LanguageId",
                table: "SteamProfiles",
                column: "LanguageId",
                principalTable: "SteamLanguages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfiles_SteamCurrencies_CurrencyId",
                table: "SteamProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfiles_SteamLanguages_LanguageId",
                table: "SteamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfiles_CurrencyId",
                table: "SteamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfiles_LanguageId",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "SteamProfiles");
        }
    }
}
