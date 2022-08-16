using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RemoveDiscordGuild : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordConfiguration");

            migrationBuilder.DropTable(
                name: "DiscordGuilds");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordGuilds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscordId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Flags = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordGuilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscordConfiguration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    List_Serialised = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscordGuildId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordConfiguration_DiscordGuilds_DiscordGuildId",
                        column: x => x.DiscordGuildId,
                        principalTable: "DiscordGuilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordConfiguration_DiscordGuildId",
                table: "DiscordConfiguration",
                column: "DiscordGuildId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordGuilds_DiscordId",
                table: "DiscordGuilds",
                column: "DiscordId",
                unique: true);
        }
    }
}
