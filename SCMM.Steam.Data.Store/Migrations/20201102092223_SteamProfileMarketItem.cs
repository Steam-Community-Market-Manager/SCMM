using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamProfileMarketItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamProfileMarketItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: true),
                    AppId = table.Column<Guid>(nullable: false),
                    DescriptionId = table.Column<Guid>(nullable: true),
                    ProfileId = table.Column<Guid>(nullable: false),
                    Flags = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamProfileMarketItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamProfileMarketItems_SteamApps_AppId",
                        column: x => x.AppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SteamProfileMarketItems_SteamAssetDescriptions_DescriptionId",
                        column: x => x.DescriptionId,
                        principalTable: "SteamAssetDescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SteamProfileMarketItems_SteamProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "SteamProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileMarketItems_AppId",
                table: "SteamProfileMarketItems",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileMarketItems_DescriptionId",
                table: "SteamProfileMarketItems",
                column: "DescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileMarketItems_ProfileId",
                table: "SteamProfileMarketItems",
                column: "ProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamProfileMarketItems");
        }
    }
}
