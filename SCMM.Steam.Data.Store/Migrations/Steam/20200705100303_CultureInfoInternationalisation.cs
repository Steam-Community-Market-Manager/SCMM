using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class CultureInfoInternationalisation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CultureName",
                table: "SteamLanguages",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CultureName",
                table: "SteamCurrencies",
                nullable: true);

            // Add all known language cultures
            SetLanguageCulture(migrationBuilder, "english", "en");

            // Add all known currency cultures
            SetCurrencyCulture(migrationBuilder, "1", "en-US");
            SetCurrencyCulture(migrationBuilder, "2", "en-GB");
            SetCurrencyCulture(migrationBuilder, "3", "de-DE");
            SetCurrencyCulture(migrationBuilder, "4", "de-CH");
            SetCurrencyCulture(migrationBuilder, "5", "ru-RU");
            SetCurrencyCulture(migrationBuilder, "6", "pl-PL");
            SetCurrencyCulture(migrationBuilder, "7", "pt-BR");
            SetCurrencyCulture(migrationBuilder, "8", "ja-JP");
            SetCurrencyCulture(migrationBuilder, "9", "nb-NO");
            SetCurrencyCulture(migrationBuilder, "10", "en-ID");
            SetCurrencyCulture(migrationBuilder, "11", "en-MY");
            SetCurrencyCulture(migrationBuilder, "12", "en-PH");
            SetCurrencyCulture(migrationBuilder, "13", "en-SG");
            SetCurrencyCulture(migrationBuilder, "14", "th-TH");
            SetCurrencyCulture(migrationBuilder, "15", "vi-VN");
            SetCurrencyCulture(migrationBuilder, "16", "ko-KR");
            SetCurrencyCulture(migrationBuilder, "17", "tr-TR");
            SetCurrencyCulture(migrationBuilder, "18", "uk-UA");
            SetCurrencyCulture(migrationBuilder, "19", "es-MX");
            SetCurrencyCulture(migrationBuilder, "20", "fr-CA");
            SetCurrencyCulture(migrationBuilder, "21", "en-AU");
            SetCurrencyCulture(migrationBuilder, "22", "en-NZ");
            SetCurrencyCulture(migrationBuilder, "23", "ii-CN");
            SetCurrencyCulture(migrationBuilder, "24", "kn-IN");
            SetCurrencyCulture(migrationBuilder, "25", "arn-CL");
            SetCurrencyCulture(migrationBuilder, "26", "es-PE");
            SetCurrencyCulture(migrationBuilder, "27", "es-CO");
            SetCurrencyCulture(migrationBuilder, "28", "nso-ZA");
            SetCurrencyCulture(migrationBuilder, "29", "zh-HK");
            SetCurrencyCulture(migrationBuilder, "30", "zh-TW");
            SetCurrencyCulture(migrationBuilder, "31", "ar-SA");
            SetCurrencyCulture(migrationBuilder, "32", "ar-AE");
            // 33 not supported
            SetCurrencyCulture(migrationBuilder, "34", "es-AR");
            SetCurrencyCulture(migrationBuilder, "35", "he-IL");
            // 36 not supported
            SetCurrencyCulture(migrationBuilder, "37", "kk-KZ");
            SetCurrencyCulture(migrationBuilder, "38", "ar-KW");
            SetCurrencyCulture(migrationBuilder, "39", "ar-QA");
            SetCurrencyCulture(migrationBuilder, "40", "es-CR");
            SetCurrencyCulture(migrationBuilder, "41", "es-UY");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CultureName",
                table: "SteamLanguages");

            migrationBuilder.DropColumn(
                name: "CultureName",
                table: "SteamCurrencies");
        }

        private void SetLanguageCulture(MigrationBuilder migrationBuilder, string steamId, string cultureName)
        {
            migrationBuilder.Sql($"UPDATE [SteamLanguages] SET [CultureName] = '{cultureName}' WHERE [SteamId] = '{steamId}'");
        }
        private void SetCurrencyCulture(MigrationBuilder migrationBuilder, string steamId, string cultureName)
        {
            migrationBuilder.Sql($"UPDATE [SteamCurrencies] SET [CultureName] = '{cultureName}' WHERE [SteamId] = '{steamId}'");
        }
    }
}
