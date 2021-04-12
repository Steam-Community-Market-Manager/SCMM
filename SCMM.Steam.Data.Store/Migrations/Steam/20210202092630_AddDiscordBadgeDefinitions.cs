using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class AddDiscordBadgeDefinitions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordBadgeDefinition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IconId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscordGuildId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordBadgeDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordBadgeDefinition_DiscordGuilds_DiscordGuildId",
                        column: x => x.DiscordGuildId,
                        principalTable: "DiscordGuilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscordBadgeDefinition_ImageData_IconId",
                        column: x => x.IconId,
                        principalTable: "ImageData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordBadgeDefinition_DiscordGuildId",
                table: "DiscordBadgeDefinition",
                column: "DiscordGuildId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordBadgeDefinition_IconId",
                table: "DiscordBadgeDefinition",
                column: "IconId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordBadgeDefinition");
        }
    }
}
