using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class DefaultSteamLanguages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddLanguage(migrationBuilder, "english", "English");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }

        private void AddLanguage(MigrationBuilder migrationBuilder, string steamId, string name)
        {
            migrationBuilder.InsertData(
                "SteamLanguages",
                new string[]
                {
                    "Id",
                    "SteamId",
                    "Name"
                },
                new string[]
                {
                    Guid.NewGuid().ToString(),
                    steamId,
                    name
                }
            );
        }
    }
}
