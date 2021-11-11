using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RefactorSteamAssetDescriptionPreviews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamAssetDescriptionPreview");

            migrationBuilder.AddColumn<string>(
                name: "Previews_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Previews_Serialised",
                table: "SteamAssetDescriptions");

            migrationBuilder.CreateTable(
                name: "SteamAssetDescriptionPreview",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SteamId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamAssetDescriptionPreview", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamAssetDescriptionPreview_SteamAssetDescriptions_DescriptionId",
                        column: x => x.DescriptionId,
                        principalTable: "SteamAssetDescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptionPreview_DescriptionId",
                table: "SteamAssetDescriptionPreview",
                column: "DescriptionId");
        }
    }
}
