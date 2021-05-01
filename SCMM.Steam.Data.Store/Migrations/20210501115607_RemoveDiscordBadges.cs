using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RemoveDiscordBadges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordBadges");

            migrationBuilder.DropTable(
                name: "DiscordBadgeDefinition");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordBadgeDefinition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IconId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordBadgeDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordBadgeDefinition_DiscordGuilds_GuildId",
                        column: x => x.GuildId,
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

            migrationBuilder.CreateTable(
                name: "DiscordBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscordUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordBadges_DiscordBadgeDefinition_BadgeDefinitionId",
                        column: x => x.BadgeDefinitionId,
                        principalTable: "DiscordBadgeDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordBadgeDefinition_GuildId",
                table: "DiscordBadgeDefinition",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordBadgeDefinition_IconId",
                table: "DiscordBadgeDefinition",
                column: "IconId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordBadges_BadgeDefinitionId",
                table: "DiscordBadges",
                column: "BadgeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordBadges_DiscordUserId_BadgeDefinitionId",
                table: "DiscordBadges",
                columns: new[] { "DiscordUserId", "BadgeDefinitionId" },
                unique: true);
        }
    }
}
