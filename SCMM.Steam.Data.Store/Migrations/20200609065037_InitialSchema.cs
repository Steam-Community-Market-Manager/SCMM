using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamApps",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    IconUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamApps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SteamAssetDescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    BackgroundColour = table.Column<string>(nullable: true),
                    ForegroundColour = table.Column<string>(nullable: true),
                    IconUrl = table.Column<string>(nullable: true),
                    IconLargeUrl = table.Column<string>(nullable: true),
                    WorkshopFileId = table.Column<Guid>(nullable: true),
                    Tags_Serialised = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamAssetDescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SteamCurrencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    PrefixText = table.Column<string>(nullable: true),
                    SuffixText = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamCurrencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SteamLanguages",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamLanguages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SteamAssetWorkshopFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: false),
                    CreatorSteamId = table.Column<string>(nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedOn = table.Column<DateTimeOffset>(nullable: false),
                    Subscriptions = table.Column<int>(nullable: false),
                    Favourited = table.Column<int>(nullable: false),
                    Views = table.Column<int>(nullable: false),
                    SteamAssetDescriptionId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamAssetWorkshopFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamAssetWorkshopFiles_SteamAssetDescriptions_SteamAssetDescriptionId",
                        column: x => x.SteamAssetDescriptionId,
                        principalTable: "SteamAssetDescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SteamMarketItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: true),
                    AppId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    DescriptionId = table.Column<Guid>(nullable: false),
                    CurrencyId = table.Column<Guid>(nullable: true),
                    Supply = table.Column<int>(nullable: false),
                    Demand = table.Column<int>(nullable: false),
                    SellLowestPrice = table.Column<int>(nullable: false),
                    SellLowestDelta = table.Column<int>(nullable: false),
                    ResellPrice = table.Column<int>(nullable: false),
                    ResellTax = table.Column<int>(nullable: false),
                    ResellProfit = table.Column<int>(nullable: false),
                    LastChecked = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamMarketItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamMarketItems_SteamApps_AppId",
                        column: x => x.AppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SteamMarketItems_SteamCurrencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "SteamCurrencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SteamMarketItems_SteamAssetDescriptions_DescriptionId",
                        column: x => x.DescriptionId,
                        principalTable: "SteamAssetDescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SteamMarketItems_BuyOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Price = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    SteamMarketItemId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamMarketItems_BuyOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamMarketItems_BuyOrders_SteamMarketItems_SteamMarketItemId",
                        column: x => x.SteamMarketItemId,
                        principalTable: "SteamMarketItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SteamMarketItems_SellOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Price = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    SteamMarketItemId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamMarketItems_SellOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamMarketItems_SellOrders_SteamMarketItems_SteamMarketItemId",
                        column: x => x.SteamMarketItemId,
                        principalTable: "SteamMarketItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_SteamAssetDescriptionId",
                table: "SteamAssetWorkshopFiles",
                column: "SteamAssetDescriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItems_AppId",
                table: "SteamMarketItems",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItems_CurrencyId",
                table: "SteamMarketItems",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItems_DescriptionId",
                table: "SteamMarketItems",
                column: "DescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItems_BuyOrders_SteamMarketItemId",
                table: "SteamMarketItems_BuyOrders",
                column: "SteamMarketItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItems_SellOrders_SteamMarketItemId",
                table: "SteamMarketItems_SellOrders",
                column: "SteamMarketItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamAssetWorkshopFiles");

            migrationBuilder.DropTable(
                name: "SteamLanguages");

            migrationBuilder.DropTable(
                name: "SteamMarketItems_BuyOrders");

            migrationBuilder.DropTable(
                name: "SteamMarketItems_SellOrders");

            migrationBuilder.DropTable(
                name: "SteamMarketItems");

            migrationBuilder.DropTable(
                name: "SteamApps");

            migrationBuilder.DropTable(
                name: "SteamCurrencies");

            migrationBuilder.DropTable(
                name: "SteamAssetDescriptions");
        }
    }
}
