using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamProfileConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamProfileConfiguration",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true),
                    List_Serialised = table.Column<string>(nullable: true),
                    SteamProfileId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamProfileConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamProfileConfiguration_SteamProfiles_SteamProfileId",
                        column: x => x.SteamProfileId,
                        principalTable: "SteamProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfileConfiguration_SteamProfileId",
                table: "SteamProfileConfiguration",
                column: "SteamProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamProfileConfiguration");
        }
    }
}
