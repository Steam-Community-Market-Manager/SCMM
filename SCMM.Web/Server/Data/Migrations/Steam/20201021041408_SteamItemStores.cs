using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamItemStores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamItemStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    AppId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Start = table.Column<DateTimeOffset>(nullable: false),
                    End = table.Column<DateTimeOffset>(nullable: true),
                    Media_Serialised = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamItemStores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamItemStores_SteamApps_AppId",
                        column: x => x.AppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SteamStoreItemItemStore",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(nullable: false),
                    StoreId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamStoreItemItemStore", x => new { x.ItemId, x.StoreId });
                    table.ForeignKey(
                        name: "FK_SteamStoreItemItemStore_SteamStoreItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "SteamStoreItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SteamStoreItemItemStore_SteamItemStores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "SteamItemStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemStores_AppId",
                table: "SteamItemStores",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamStoreItemItemStore_StoreId",
                table: "SteamStoreItemItemStore",
                column: "StoreId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamStoreItemItemStore");

            migrationBuilder.DropTable(
                name: "SteamItemStores");
        }
    }
}
