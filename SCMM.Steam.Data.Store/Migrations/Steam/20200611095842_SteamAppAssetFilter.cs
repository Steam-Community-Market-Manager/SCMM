using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class SteamAppAssetFilter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamAssetFilter",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Options_Serialised = table.Column<string>(nullable: true),
                    SteamAppId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamAssetFilter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamAssetFilter_SteamApps_SteamAppId",
                        column: x => x.SteamAppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetFilter_SteamAppId",
                table: "SteamAssetFilter",
                column: "SteamAppId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamAssetFilter");
        }
    }
}
