using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamMarketItemActivity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamMarketItemActivity",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(nullable: false),
                    Movement = table.Column<long>(nullable: false),
                    ItemId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamMarketItemActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamMarketItemActivity_SteamMarketItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "SteamMarketItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamMarketItemActivity_ItemId",
                table: "SteamMarketItemActivity",
                column: "ItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamMarketItemActivity");
        }
    }
}
