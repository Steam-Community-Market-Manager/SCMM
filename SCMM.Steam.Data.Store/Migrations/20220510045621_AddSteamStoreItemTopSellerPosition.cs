using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamStoreItemTopSellerPosition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamStoreItemTopSellerPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamStoreItemTopSellerPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamStoreItemTopSellerPositions_SteamAssetDescriptions_DescriptionId",
                        column: x => x.DescriptionId,
                        principalTable: "SteamAssetDescriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamStoreItemTopSellerPositions_DescriptionId",
                table: "SteamStoreItemTopSellerPositions",
                column: "DescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamStoreItemTopSellerPositions_Timestamp_DescriptionId_Position_Total",
                table: "SteamStoreItemTopSellerPositions",
                columns: new[] { "Timestamp", "DescriptionId", "Position", "Total" },
                unique: true,
                filter: "[DescriptionId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamStoreItemTopSellerPositions");
        }
    }
}
