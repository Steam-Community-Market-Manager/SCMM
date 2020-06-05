using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class CreateSteamSchema : Migration
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
                name: "SteamItemDescription",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    BackgroundColour = table.Column<string>(nullable: true),
                    ForegroundColour = table.Column<string>(nullable: true),
                    IconUrl = table.Column<string>(nullable: true),
                    IconLargeUrl = table.Column<string>(nullable: true),
                    Tags_Serialised = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamItemDescription", x => x.Id);
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
                name: "SteamItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: true),
                    AppId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    DescriptionId = table.Column<Guid>(nullable: false),
                    LastChecked = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamItems_SteamApps_AppId",
                        column: x => x.AppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SteamItems_SteamItemDescription_DescriptionId",
                        column: x => x.DescriptionId,
                        principalTable: "SteamItemDescription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SteamItemOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Price = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    BuyOrderItemId = table.Column<Guid>(nullable: true),
                    SellOrderItemId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamItemOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamItemOrder_SteamItems_BuyOrderItemId",
                        column: x => x.BuyOrderItemId,
                        principalTable: "SteamItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SteamItemOrder_SteamItems_SellOrderItemId",
                        column: x => x.SellOrderItemId,
                        principalTable: "SteamItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemOrder_BuyOrderItemId",
                table: "SteamItemOrder",
                column: "BuyOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemOrder_SellOrderItemId",
                table: "SteamItemOrder",
                column: "SellOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItems_AppId",
                table: "SteamItems",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItems_DescriptionId",
                table: "SteamItems",
                column: "DescriptionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamCurrencies");

            migrationBuilder.DropTable(
                name: "SteamItemOrder");

            migrationBuilder.DropTable(
                name: "SteamLanguages");

            migrationBuilder.DropTable(
                name: "SteamItems");

            migrationBuilder.DropTable(
                name: "SteamApps");

            migrationBuilder.DropTable(
                name: "SteamItemDescription");
        }
    }
}
