using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddImageData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AvatarId",
                table: "SteamProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImageId",
                table: "SteamAssetWorkshopFiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IconId",
                table: "SteamAssetDescriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IconId",
                table: "SteamApps",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImageData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MineType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ValueLarge = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfiles_AvatarId",
                table: "SteamProfiles",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_ImageId",
                table: "SteamAssetWorkshopFiles",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_IconId",
                table: "SteamAssetDescriptions",
                column: "IconId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamApps_IconId",
                table: "SteamApps",
                column: "IconId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamApps_ImageData_IconId",
                table: "SteamApps",
                column: "IconId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_IconId",
                table: "SteamAssetDescriptions",
                column: "IconId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_ImageData_ImageId",
                table: "SteamAssetWorkshopFiles",
                column: "ImageId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfiles_ImageData_AvatarId",
                table: "SteamProfiles",
                column: "AvatarId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamApps_ImageData_IconId",
                table: "SteamApps");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_IconId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_ImageData_ImageId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfiles_ImageData_AvatarId",
                table: "SteamProfiles");

            migrationBuilder.DropTable(
                name: "ImageData");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfiles_AvatarId",
                table: "SteamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetWorkshopFiles_ImageId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_IconId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamApps_IconId",
                table: "SteamApps");

            migrationBuilder.DropColumn(
                name: "AvatarId",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropColumn(
                name: "IconId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "IconId",
                table: "SteamApps");
        }
    }
}
