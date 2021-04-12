using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class AddDiscordBadges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordBadgeDefinition_DiscordGuilds_DiscordGuildId",
                table: "DiscordBadgeDefinition");

            migrationBuilder.DropIndex(
                name: "IX_DiscordBadgeDefinition_DiscordGuildId",
                table: "DiscordBadgeDefinition");

            migrationBuilder.DropColumn(
                name: "DiscordGuildId",
                table: "DiscordBadgeDefinition");

            migrationBuilder.AddColumn<Guid>(
                name: "GuildId",
                table: "DiscordBadgeDefinition",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "DiscordBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscordUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BadgeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                name: "IX_DiscordBadges_BadgeDefinitionId",
                table: "DiscordBadges",
                column: "BadgeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordBadges_DiscordUserId_BadgeDefinitionId",
                table: "DiscordBadges",
                columns: new[] { "DiscordUserId", "BadgeDefinitionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordBadgeDefinition_DiscordGuilds_GuildId",
                table: "DiscordBadgeDefinition",
                column: "GuildId",
                principalTable: "DiscordGuilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordBadgeDefinition_DiscordGuilds_GuildId",
                table: "DiscordBadgeDefinition");

            migrationBuilder.DropTable(
                name: "DiscordBadges");

            migrationBuilder.DropIndex(
                name: "IX_DiscordBadgeDefinition_GuildId",
                table: "DiscordBadgeDefinition");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "DiscordBadgeDefinition");

            migrationBuilder.AddColumn<Guid>(
                name: "DiscordGuildId",
                table: "DiscordBadgeDefinition",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordBadgeDefinition_DiscordGuildId",
                table: "DiscordBadgeDefinition",
                column: "DiscordGuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordBadgeDefinition_DiscordGuilds_DiscordGuildId",
                table: "DiscordBadgeDefinition",
                column: "DiscordGuildId",
                principalTable: "DiscordGuilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
