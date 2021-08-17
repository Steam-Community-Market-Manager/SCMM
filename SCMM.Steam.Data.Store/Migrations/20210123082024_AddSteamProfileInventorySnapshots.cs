using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamProfileInventorySnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamProfileInventorySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvestedValue = table.Column<long>(type: "bigint", nullable: false),
                    MarketValue = table.Column<long>(type: "bigint", nullable: false),
                    TotalItems = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamProfileInventorySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamProfileInventorySnapshots_SteamCurrencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "SteamCurrencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SteamProfileInventorySnapshots_SteamProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "SteamProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileInventorySnapshots_CurrencyId",
                table: "SteamProfileInventorySnapshots",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileInventorySnapshots_ProfileId_Timestamp",
                table: "SteamProfileInventorySnapshots",
                columns: new[] { "ProfileId", "Timestamp" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamProfileInventorySnapshots");
        }
    }
}
