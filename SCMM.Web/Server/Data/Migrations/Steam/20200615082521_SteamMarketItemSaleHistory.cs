using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamMarketItemSaleHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllTimeHighestValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AllTimeHighestValueOn",
                table: "SteamMarketItems",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AllTimeLowestValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AllTimeLowestValueOn",
                table: "SteamMarketItems",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OriginalValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SwingValue",
                table: "SteamMarketItems",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeOnMarket",
                table: "SteamMarketItems",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SteamMarketItemSale",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(nullable: false),
                    Price = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    SteamMarketItemId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamMarketItemSale", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamMarketItemSale_SteamMarketItems_SteamMarketItemId",
                        column: x => x.SteamMarketItemId,
                        principalTable: "SteamMarketItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemSale_SteamMarketItemId",
                table: "SteamMarketItemSale",
                column: "SteamMarketItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamMarketItemSale");

            migrationBuilder.DropColumn(
                name: "AllTimeHighestValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "AllTimeHighestValueOn",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "AllTimeLowestValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "AllTimeLowestValueOn",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "CurrentValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "OriginalValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "SwingValue",
                table: "SteamMarketItems");

            migrationBuilder.DropColumn(
                name: "TimeOnMarket",
                table: "SteamMarketItems");
        }
    }
}
