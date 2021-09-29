using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class CleanUpModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamApps_FileData_IconId",
                table: "SteamApps");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamApps_FileData_IconLargeId",
                table: "SteamApps");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfiles_FileData_AvatarId",
                table: "SteamProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamProfiles_FileData_AvatarLargeId",
                table: "SteamProfiles");

            migrationBuilder.DropTable(
                name: "SteamAssetWorkshopFiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfiles_AvatarId",
                table: "SteamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamProfiles_AvatarLargeId",
                table: "SteamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamApps_IconId",
                table: "SteamApps");

            migrationBuilder.DropIndex(
                name: "IX_SteamApps_IconLargeId",
                table: "SteamApps");

            migrationBuilder.DropColumn(
                name: "AvatarId",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "AvatarLargeId",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "LastSnapshotInventoryOn",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "Preferences_Serialised",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "IconId",
                table: "SteamApps");

            migrationBuilder.DropColumn(
                name: "IconLargeId",
                table: "SteamApps");

            migrationBuilder.RenameColumn(
                name: "Open24hrValue",
                table: "SteamMarketItems",
                newName: "Stable24hrValue");

            migrationBuilder.RenameColumn(
                name: "FirstSeenOn",
                table: "SteamMarketItems",
                newName: "FirstSaleOn");

            migrationBuilder.Sql(@"
                UPDATE [SteamMarketItems] SET [LastSaleValue] = 0 WHERE [LastSaleValue] IS NULL
            ");

            migrationBuilder.AlterColumn<long>(
                name: "LastSaleValue",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Stable24hrValue",
                table: "SteamMarketItems",
                newName: "Open24hrValue");

            migrationBuilder.RenameColumn(
                name: "FirstSaleOn",
                table: "SteamMarketItems",
                newName: "FirstSeenOn");

            migrationBuilder.AddColumn<Guid>(
                name: "AvatarId",
                table: "SteamProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AvatarLargeId",
                table: "SteamProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "SteamProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSnapshotInventoryOn",
                table: "SteamProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Preferences_Serialised",
                table: "SteamProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "LastSaleValue",
                table: "SteamMarketItems",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<Guid>(
                name: "IconId",
                table: "SteamApps",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IconLargeId",
                table: "SteamApps",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SteamAssetWorkshopFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BanReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Favourited = table.Column<int>(type: "int", nullable: false),
                    ImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subscriptions = table.Column<long>(type: "bigint", nullable: false),
                    TimeAccepted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TimeCreated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimeRefreshed = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TimeUpdated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    WorkshopFileId = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamAssetWorkshopFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamAssetWorkshopFiles_FileData_ImageId",
                        column: x => x.ImageId,
                        principalTable: "FileData",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SteamAssetWorkshopFiles_SteamApps_AppId",
                        column: x => x.AppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "SteamProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfiles_AvatarId",
                table: "SteamProfiles",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamProfiles_AvatarLargeId",
                table: "SteamProfiles",
                column: "AvatarLargeId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamApps_IconId",
                table: "SteamApps",
                column: "IconId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamApps_IconLargeId",
                table: "SteamApps",
                column: "IconLargeId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_AppId",
                table: "SteamAssetWorkshopFiles",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_CreatorId",
                table: "SteamAssetWorkshopFiles",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_ImageId",
                table: "SteamAssetWorkshopFiles",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_WorkshopFileId",
                table: "SteamAssetWorkshopFiles",
                column: "WorkshopFileId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamApps_FileData_IconId",
                table: "SteamApps",
                column: "IconId",
                principalTable: "FileData",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamApps_FileData_IconLargeId",
                table: "SteamApps",
                column: "IconLargeId",
                principalTable: "FileData",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfiles_FileData_AvatarId",
                table: "SteamProfiles",
                column: "AvatarId",
                principalTable: "FileData",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamProfiles_FileData_AvatarLargeId",
                table: "SteamProfiles",
                column: "AvatarLargeId",
                principalTable: "FileData",
                principalColumn: "Id");
        }
    }
}
