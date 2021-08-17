using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamInternationalisation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "SteamLanguages",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateMultiplier",
                table: "SteamCurrencies",
                type: "decimal(29,21)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "SteamCurrencies",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Scale",
                table: "SteamCurrencies",
                nullable: false,
                defaultValue: 0);

            // Fix up currencies from previous migrations
            migrationBuilder.Sql("UPDATE [SteamCurrencies] SET [SteamId] = '1' WHERE [SteamId] = '2'");
            migrationBuilder.Sql("UPDATE [SteamCurrencies] SET [PrefixText] = '$' WHERE [SteamId] IN ('1', '22')");
            migrationBuilder.Sql("UPDATE [SteamCurrencies] SET [SuffixText] = '' WHERE [SteamId] IN ('1', '22')");
            migrationBuilder.Sql("UPDATE [SteamCurrencies] SET [Scale] = 2 WHERE [SteamId] IN ('1', '22')");

            // Add all known currencies
            // 1 USD already exists
            AddCurrency(migrationBuilder, "2", "GBP", "£", "", 2);
            AddCurrency(migrationBuilder, "3", "EUR", "€", "", 2);
            AddCurrency(migrationBuilder, "4", "CHF", "", " CHF", 2);
            AddCurrency(migrationBuilder, "5", "RUB", "₽", "", 2);
            AddCurrency(migrationBuilder, "6", "PLN", "zł", "", 2);
            AddCurrency(migrationBuilder, "7", "BRL", "R$", "", 2);
            AddCurrency(migrationBuilder, "8", "JPY", "¥", "", 0);
            AddCurrency(migrationBuilder, "9", "NOK", "kr.", "", 2);
            AddCurrency(migrationBuilder, "10", "IDR", "", " Rp", 2);
            AddCurrency(migrationBuilder, "11", "MYR", "", " RM", 2);
            AddCurrency(migrationBuilder, "12", "PHP", "₱", "", 2);
            AddCurrency(migrationBuilder, "13", "SGD", "$", "", 2);
            AddCurrency(migrationBuilder, "14", "THB", "฿", "", 2);
            AddCurrency(migrationBuilder, "15", "VND", "₫", "", 0);
            AddCurrency(migrationBuilder, "16", "KRW", "₩", "", 0);
            AddCurrency(migrationBuilder, "17", "TRY", "₺", "", 2);
            AddCurrency(migrationBuilder, "18", "UAH", "₴", "", 2);
            AddCurrency(migrationBuilder, "19", "MXN", "$", "", 2);
            AddCurrency(migrationBuilder, "20", "CAD", "$", "", 2);
            AddCurrency(migrationBuilder, "21", "AUD", "$", "", 2);
            // 22 NZD already exists
            AddCurrency(migrationBuilder, "23", "CNY", "¥", "", 2);
            AddCurrency(migrationBuilder, "24", "INR", "₹", "", 2);
            AddCurrency(migrationBuilder, "25", "CLP", "$", "", 0);
            AddCurrency(migrationBuilder, "26", "PEN", "S/.", "", 2);
            AddCurrency(migrationBuilder, "27", "COP", "$", "", 2);
            AddCurrency(migrationBuilder, "28", "ZAR", "R", "", 2);
            AddCurrency(migrationBuilder, "29", "HKD", "$", "", 2);
            AddCurrency(migrationBuilder, "30", "TWD", "NT$", "", 2);
            AddCurrency(migrationBuilder, "31", "SAR", "﷼", "", 2);
            AddCurrency(migrationBuilder, "32", "AED", "د.إ", "", 2);
            // 33 not supported
            AddCurrency(migrationBuilder, "34", "ARS", "$", "", 2);
            AddCurrency(migrationBuilder, "35", "ILS", "₪", "", 2);
            // 36 not supported
            AddCurrency(migrationBuilder, "37", "KZT", "лв", "", 2);
            AddCurrency(migrationBuilder, "38", "KWD", "", " KD", 3);
            AddCurrency(migrationBuilder, "39", "QAR", "﷼", "", 2);
            AddCurrency(migrationBuilder, "40", "CRC", "₡", "", 2);
            AddCurrency(migrationBuilder, "41", "UYU", "$U", "", 2);

            // Set default currency and language
            migrationBuilder.Sql("UPDATE [SteamCurrencies] SET [IsDefault] = 1 WHERE [SteamId] = '1'");
            migrationBuilder.Sql("UPDATE [SteamLanguages] SET [IsDefault] = 1 WHERE [SteamId] = 'english'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "SteamLanguages");

            migrationBuilder.DropColumn(
                name: "ExchangeRateMultiplier",
                table: "SteamCurrencies");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "SteamCurrencies");

            migrationBuilder.DropColumn(
                name: "Scale",
                table: "SteamCurrencies");
        }

        private void AddCurrency(MigrationBuilder migrationBuilder, string steamId, string name, string prefixText, string suffixText, int scale)
        {
            migrationBuilder.InsertData(
                "SteamCurrencies",
                new string[]
                {
                    "Id",
                    "SteamId",
                    "Name",
                    "PrefixText",
                    "SuffixText",
                    "Scale"
                },
                new string[]
                {
                    Guid.NewGuid().ToString(),
                    steamId,
                    name,
                    prefixText,
                    suffixText,
                    scale.ToString()
                }
            );
        }
    }
}
