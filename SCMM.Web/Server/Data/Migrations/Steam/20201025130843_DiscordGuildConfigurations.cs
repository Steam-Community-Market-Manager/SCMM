using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class DiscordGuildConfigurations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordGuilds",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DiscordId = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordGuilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscordConfiguration",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true),
                    List_Serialised = table.Column<string>(nullable: true),
                    DiscordGuildId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordConfiguration_DiscordGuilds_DiscordGuildId",
                        column: x => x.DiscordGuildId,
                        principalTable: "DiscordGuilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordConfiguration_DiscordGuildId",
                table: "DiscordConfiguration",
                column: "DiscordGuildId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscordConfiguration");

            migrationBuilder.DropTable(
                name: "DiscordGuilds");
        }
    }
}
