using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamItemHierarchy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SellLowestDelta",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SellLowestPrice",
                table: "SteamMarketItems");

            migrationBuilder.AddColumn<int>(
                name: "BuyNowPrice",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BuyNowPriceDelta",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IconLargeUrl",
                table: "SteamApps",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SteamInventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: true),
                    AppId = table.Column<Guid>(nullable: false),
                    DescriptionId = table.Column<Guid>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: true),
                    BuyPrice = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamInventoryItems_SteamApps_AppId",
                        column: x => x.AppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SteamInventoryItems_SteamCurrencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "SteamCurrencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SteamInventoryItems_SteamAssetDescriptions_DescriptionId",
                        column: x => x.DescriptionId,
                        principalTable: "SteamAssetDescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SteamStoreItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: true),
                    AppId = table.Column<Guid>(nullable: false),
                    DescriptionId = table.Column<Guid>(nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: true),
                    StorePrice = table.Column<int>(nullable: false),
                    FirstReleasedOn = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamStoreItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamStoreItems_SteamApps_AppId",
                        column: x => x.AppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SteamStoreItems_SteamCurrencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "SteamCurrencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SteamStoreItems_SteamAssetDescriptions_DescriptionId",
                        column: x => x.DescriptionId,
                        principalTable: "SteamAssetDescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamInventoryItems_AppId",
                table: "SteamInventoryItems",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamInventoryItems_CurrencyId",
                table: "SteamInventoryItems",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamInventoryItems_DescriptionId",
                table: "SteamInventoryItems",
                column: "DescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamStoreItems_AppId",
                table: "SteamStoreItems",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamStoreItems_CurrencyId",
                table: "SteamStoreItems",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamStoreItems_DescriptionId",
                table: "SteamStoreItems",
                column: "DescriptionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamInventoryItems");

            migrationBuilder.DropTable(
                name: "SteamStoreItems");

            migrationBuilder.DropColumn(
                name: "BuyNowPrice",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "BuyNowPriceDelta",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "IconLargeUrl",
                table: "SteamApps");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SteamMarketItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SellLowestDelta",
                table: "SteamMarketItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SellLowestPrice",
                table: "SteamMarketItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
