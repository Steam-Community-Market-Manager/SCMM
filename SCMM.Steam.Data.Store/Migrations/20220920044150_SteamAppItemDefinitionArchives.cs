using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamAppItemDefinitionArchives : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamItemDefinitionsArchive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Digest = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemDefinitions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimePublished = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamItemDefinitionsArchive", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamItemDefinitionsArchive_SteamApps_AppId",
                        column: x => x.AppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemDefinitionsArchive_AppId",
                table: "SteamItemDefinitionsArchive",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamItemDefinitionsArchive_Digest",
                table: "SteamItemDefinitionsArchive",
                column: "Digest",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamItemDefinitionsArchive");
        }
    }
}
