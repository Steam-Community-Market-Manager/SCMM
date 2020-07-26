using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamAssetDescriptions_SteamAssetDescriptionId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetWorkshopFiles_SteamAssetDescriptionId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropColumn(
                name: "CreatorSteamId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkshopFileId",
                table: "SteamAssetDescriptions",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [SteamAssetDescriptions] SET [WorkshopFileId] = awf.Id " +
                "FROM [SteamAssetDescriptions] ad INNER JOIN [SteamAssetWorkshopFiles] awf on awf.SteamAssetDescriptionId = ad.Id"
            );

            migrationBuilder.DropColumn(
                name: "SteamAssetDescriptionId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.AddColumn<Guid>(
                name: "SteamProfileId",
                table: "SteamInventoryItems",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "SteamAssetWorkshopFiles",
                nullable: true,
                defaultValue: null);

            migrationBuilder.CreateTable(
                name: "SteamProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SteamId = table.Column<string>(nullable: true),
                    ProfileId = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    AvatarUrl = table.Column<string>(nullable: true),
                    AvatarLargeUrl = table.Column<string>(nullable: true),
                    Country = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamInventoryItems_SteamProfileId",
                table: "SteamInventoryItems",
                column: "SteamProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_CreatorId",
                table: "SteamAssetWorkshopFiles",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_WorkshopFileId",
                table: "SteamAssetDescriptions",
                column: "WorkshopFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_SteamAssetWorkshopFiles_WorkshopFileId",
                table: "SteamAssetDescriptions",
                column: "WorkshopFileId",
                principalTable: "SteamAssetWorkshopFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles",
                column: "CreatorId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_SteamProfileId",
                table: "SteamInventoryItems",
                column: "SteamProfileId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_SteamAssetWorkshopFiles_WorkshopFileId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamInventoryItems_SteamProfiles_SteamProfileId",
                table: "SteamInventoryItems");

            migrationBuilder.DropTable(
                name: "SteamProfiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamInventoryItems_SteamProfileId",
                table: "SteamInventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetWorkshopFiles_CreatorId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_WorkshopFileId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "SteamProfileId",
                table: "SteamInventoryItems");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropColumn(
                name: "WorkshopFileId",
                table: "SteamAssetDescriptions");

            migrationBuilder.AddColumn<string>(
                name: "CreatorSteamId",
                table: "SteamAssetWorkshopFiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SteamAssetDescriptionId",
                table: "SteamAssetWorkshopFiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_SteamAssetDescriptionId",
                table: "SteamAssetWorkshopFiles",
                column: "SteamAssetDescriptionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamAssetDescriptions_SteamAssetDescriptionId",
                table: "SteamAssetWorkshopFiles",
                column: "SteamAssetDescriptionId",
                principalTable: "SteamAssetDescriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
