using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RefactorAssetWorkshopFile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_SteamProfiles_CreatorId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetWorkshopFiles_SteamId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.RenameColumn(
                name: "SteamId",
                table: "SteamAssetWorkshopFiles",
                newName: "WorkshopFileId");

            migrationBuilder.DropColumn(
                name: "SubscriptionsGraph_Serialised",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.RenameColumn(
                name: "UpdatedOn",
                table: "SteamAssetWorkshopFiles",
                newName: "TimeUpdated");

            migrationBuilder.RenameColumn(
                name: "LastCheckedOn",
                table: "SteamAssetWorkshopFiles",
                newName: "TimeRefreshed");

            migrationBuilder.RenameColumn(
                name: "CreatedOn",
                table: "SteamAssetWorkshopFiles",
                newName: "TimeCreated");

            migrationBuilder.RenameColumn(
                name: "AcceptedOn",
                table: "SteamAssetWorkshopFiles",
                newName: "TimeAccepted");

            migrationBuilder.AlterColumn<long>(
                name: "Subscriptions",
                table: "SteamAssetWorkshopFiles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "WorkshopFileId",
                table: "SteamAssetWorkshopFiles",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_WorkshopFileId",
                table: "SteamAssetWorkshopFiles",
                column: "WorkshopFileId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_SteamProfiles_CreatorId",
                table: "SteamAssetDescriptions",
                column: "CreatorId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles",
                column: "CreatorId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_SteamProfiles_CreatorId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetWorkshopFiles_WorkshopFileId",
                table: "SteamAssetWorkshopFiles");

            migrationBuilder.RenameColumn(
                name: "WorkshopFileId",
                table: "SteamAssetWorkshopFiles",
                newName: "SteamId");

            migrationBuilder.RenameColumn(
                name: "TimeUpdated",
                table: "SteamAssetWorkshopFiles",
                newName: "UpdatedOn");

            migrationBuilder.RenameColumn(
                name: "TimeRefreshed",
                table: "SteamAssetWorkshopFiles",
                newName: "LastCheckedOn");

            migrationBuilder.RenameColumn(
                name: "TimeCreated",
                table: "SteamAssetWorkshopFiles",
                newName: "CreatedOn");

            migrationBuilder.RenameColumn(
                name: "TimeAccepted",
                table: "SteamAssetWorkshopFiles",
                newName: "AcceptedOn");

            migrationBuilder.AlterColumn<int>(
                name: "Subscriptions",
                table: "SteamAssetWorkshopFiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "SteamId",
                table: "SteamAssetWorkshopFiles",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionsGraph_Serialised",
                table: "SteamAssetWorkshopFiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetWorkshopFiles_SteamId",
                table: "SteamAssetWorkshopFiles",
                column: "SteamId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_SteamProfiles_CreatorId",
                table: "SteamAssetDescriptions",
                column: "CreatorId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetWorkshopFiles_SteamProfiles_CreatorId",
                table: "SteamAssetWorkshopFiles",
                column: "CreatorId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
