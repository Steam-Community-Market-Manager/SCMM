using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamCurrencyExchangeRate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamCurrencyExchangeRates",
                columns: table => new
                {
                    CurrencyId = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExchangeRateMultiplier = table.Column<decimal>(type: "decimal(29,21)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamCurrencyExchangeRates", x => new { x.CurrencyId, x.Timestamp });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamCurrencyExchangeRates");
        }
    }
}
