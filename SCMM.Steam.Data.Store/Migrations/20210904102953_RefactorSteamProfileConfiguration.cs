using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RefactorSteamProfileConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfileConfiguration_SteamProfiles_SteamProfileId",
                table: "SteamProfileConfiguration");

            migrationBuilder.DropTable(
                name: "SteamProfileInventorySnapshots");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfileConfiguration_SteamProfileId",
                table: "SteamProfileConfiguration");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "SteamProfileId",
                table: "SteamProfileConfiguration");

            migrationBuilder.AddColumn<string>(
                name: "Preferences_Serialised",
                table: "SteamProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Preferences_Serialised",
                table: "SteamProfiles");

            migrationBuilder.AddColumn<byte>(
                name: "Flags",
                table: "SteamProfiles",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<Guid>(
                name: "SteamProfileId",
                table: "SteamProfileConfiguration",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SteamProfileInventorySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvestedValue = table.Column<long>(type: "bigint", nullable: false),
                    MarketValue = table.Column<long>(type: "bigint", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                name: "IX_SteamProfileConfiguration_SteamProfileId",
                table: "SteamProfileConfiguration",
                column: "SteamProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileInventorySnapshots_CurrencyId",
                table: "SteamProfileInventorySnapshots",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileInventorySnapshots_ProfileId_Timestamp",
                table: "SteamProfileInventorySnapshots",
                columns: new[] { "ProfileId", "Timestamp" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfileConfiguration_SteamProfiles_SteamProfileId",
                table: "SteamProfileConfiguration",
                column: "SteamProfileId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
