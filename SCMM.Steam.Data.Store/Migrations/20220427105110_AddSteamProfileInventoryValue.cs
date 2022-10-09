using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamProfileInventoryValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTotalInventoryItems",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "LastTotalInventoryValue",
                table: "SteamProfiles");

            migrationBuilder.CreateTable(
                name: "SteamProfileInventoryValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Items = table.Column<int>(type: "int", nullable: false),
                    MarketValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamProfileInventoryValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamProfileInventoryValues_SteamApps_AppId",
                        column: x => x.AppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SteamProfileInventoryValues_SteamCurrencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "SteamCurrencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SteamProfileInventoryValues_SteamProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "SteamProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileInventoryValues_AppId",
                table: "SteamProfileInventoryValues",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileInventoryValues_CurrencyId",
                table: "SteamProfileInventoryValues",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileInventoryValues_ProfileId",
                table: "SteamProfileInventoryValues",
                column: "ProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamProfileInventoryValues");

            migrationBuilder.AddColumn<int>(
                name: "LastTotalInventoryItems",
                table: "SteamProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "LastTotalInventoryValue",
                table: "SteamProfiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
