using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RefactorSteamAssetDescriptionWorkshopFileDataAsUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_WorkshopFileDataId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_WorkshopFileDataId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "WorkshopFileDataId",
                table: "SteamAssetDescriptions");

            migrationBuilder.AddColumn<string>(
                name: "WorkshopFileUrl",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkshopFileUrl",
                table: "SteamAssetDescriptions");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkshopFileDataId",
                table: "SteamAssetDescriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_WorkshopFileDataId",
                table: "SteamAssetDescriptions",
                column: "WorkshopFileDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_FileData_WorkshopFileDataId",
                table: "SteamAssetDescriptions",
                column: "WorkshopFileDataId",
                principalTable: "FileData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
