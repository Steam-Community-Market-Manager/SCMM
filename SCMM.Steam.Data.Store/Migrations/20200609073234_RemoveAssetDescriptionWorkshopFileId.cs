using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RemoveAssetDescriptionWorkshopFileId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkshopFileId",
                table: "SteamAssetDescriptions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkshopFileId",
                table: "SteamAssetDescriptions",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
