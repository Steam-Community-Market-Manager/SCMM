using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class DefaultSteamCurrencies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddCurrency(migrationBuilder, "2", "USD", "US$", null);
            AddCurrency(migrationBuilder, "22", "NZD", "NZ$", null);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }

        private void AddCurrency(MigrationBuilder migrationBuilder, string steamId, string name, string prefixText, string suffixText)
        {
            migrationBuilder.InsertData(
                "SteamCurrencies",
                new string[]
                {
                    "Id",
                    "SteamId",
                    "Name",
                    "PrefixText",
                    "SuffixText"
                },
                new string[]
                {
                    Guid.NewGuid().ToString(),
                    steamId,
                    name,
                    prefixText,
                    suffixText
                }
            );
        }
    }
}
