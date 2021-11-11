using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamAssetDescriptionPreviews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviewContentId",
                table: "SteamAssetDescriptions");

            migrationBuilder.AddColumn<decimal>(
                name: "VoteRatio",
                table: "SteamAssetDescriptions",
                type: "decimal(20,20)",
                precision: 20,
                scale: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SteamAssetDescriptionPreview",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SteamId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    DescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Index = table.Column<int>(type: "int", nullable: false)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamAssetDescriptionPreview");

            migrationBuilder.DropColumn(
                name: "VoteRatio",
                table: "SteamAssetDescriptions");

            migrationBuilder.AddColumn<decimal>(
                name: "PreviewContentId",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: true);
        }
    }
}
