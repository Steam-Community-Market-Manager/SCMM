using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class MergeAssetDescriptionAndWorkshopFile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_SteamAssetWorkshopFiles_WorkshopFileId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_SteamId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_WorkshopFileId",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameColumn(
                name: "SteamId",
                table: "SteamAssetDescriptions",
                newName: "AssetId");

            migrationBuilder.AlterColumn<decimal>(
                name: "AssetId",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.RenameColumn(
                name: "LastCheckedOn",
                table: "SteamAssetDescriptions",
                newName: "TimeUpdated");

            migrationBuilder.RenameColumn(
                name: "Flags",
                table: "SteamAssetDescriptions",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "WorkshopFileId",
                table: "SteamAssetDescriptions",
                newName: "AssetWorkshopFileId");

            migrationBuilder.AddColumn<decimal>(
                name: "WorkshopFileId",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BanReason",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BreaksDownInto_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CraftingRequirements_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "SteamAssetDescriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentSubscriptions",
                table: "SteamAssetDescriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImageId",
                table: "SteamAssetDescriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBreakable",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCommodity",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCraftable",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMarketable",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTradable",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "MarketableRestrictionDays",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameHash",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NameId",
                table: "SteamAssetDescriptions",
                type: "decimal(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeAccepted",
                table: "SteamAssetDescriptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeChecked",
                table: "SteamAssetDescriptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeCreated",
                table: "SteamAssetDescriptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalSubscriptions",
                table: "SteamAssetDescriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TradableRestrictionDays",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_AssetId",
                table: "SteamAssetDescriptions",
                column: "AssetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_CreatorId",
                table: "SteamAssetDescriptions",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_ImageId",
                table: "SteamAssetDescriptions",
                column: "ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_ImageId",
                table: "SteamAssetDescriptions",
                column: "ImageId",
                principalTable: "ImageData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SteamAssetDescriptions_SteamProfiles_CreatorId",
                table: "SteamAssetDescriptions",
                column: "CreatorId",
                principalTable: "SteamProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                UPDATE [SteamAssetDescriptions] SET [Type] = 0 WHERE[AssetWorkshopFileId] IS NULL
            ");
            migrationBuilder.Sql(@"
                UPDATE [SteamAssetDescriptions] SET [Type] = 1 WHERE[AssetWorkshopFileId] IS NOT NULL
            ");
            migrationBuilder.Sql(@"
                UPDATE Asset
                SET
                    Asset.WorkshopFileId = CONVERT(DECIMAL(20, 0), Workshop.SteamId),
	                Asset.BanReason = Workshop.BanReason,
	                Asset.CreatorId = Workshop.CreatorId,
	                Asset.TotalSubscriptions = Workshop.Subscriptions,
	                Asset.CurrentSubscriptions = Workshop.Subscriptions,
	                Asset.ImageId = Workshop.ImageId,
	                Asset.ImageUrl = Workshop.ImageUrl,
	                Asset.NameHash = Workshop.Name,
	                Asset.NameId = CONVERT(DECIMAL(20, 0), Market.SteamId),
	                Asset.TimeAccepted = Workshop.AcceptedOn,
	                Asset.TimeCreated = Workshop.CreatedOn,
	                Asset.TimeUpdated = Workshop.UpdatedOn,
	                Asset.TimeChecked = Workshop.LastCheckedOn
                FROM [SteamAssetDescriptions] Asset
                   INNER JOIN[SteamAssetWorkshopFiles] Workshop ON Workshop.Id = Asset.AssetWorkshopFileId
                   RIGHT OUTER JOIN[SteamMarketItems] Market ON Market.DescriptionId = Asset.Id
            ");
            migrationBuilder.Sql(@"
                UPDATE [SteamAssetDescriptions] SET [Tags_Serialised] = NULL, [Description] = NULL, [TimeChecked] = NULL
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_ImageData_ImageId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SteamAssetDescriptions_SteamProfiles_CreatorId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_AssetId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_CreatorId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_SteamAssetDescriptions_ImageId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "BanReason",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "BreaksDownInto_Serialised",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "CraftingRequirements_Serialised",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "CurrentSubscriptions",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "IsBanned",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "IsBreakable",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "IsCommodity",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "IsCraftable",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "IsMarketable",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "IsTradable",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "MarketableRestrictionDays",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "NameHash",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "NameId",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "TimeAccepted",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "TimeChecked",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "TimeCreated",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "TotalSubscriptions",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "TradableRestrictionDays",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "SteamAssetDescriptions",
                newName: "Flags");

            migrationBuilder.RenameColumn(
                name: "TimeUpdated",
                table: "SteamAssetDescriptions",
                newName: "LastCheckedOn");

            migrationBuilder.DropColumn(
                name: "WorkshopFileId",
                table: "SteamAssetDescriptions");

            migrationBuilder.RenameColumn(
                name: "AssetWorkshopFileId",
                table: "SteamAssetDescriptions",
                newName: "WorkshopFileId");

            migrationBuilder.AlterColumn<string>(
                name: "AssetId",
                table: "SteamAssetDescriptions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(decimal),
                oldType: "decimal(20,0)");

            migrationBuilder.RenameColumn(
                name: "AssetId",
                table: "SteamAssetDescriptions",
                newName: "SteamId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamAssetDescriptions_SteamId",
                table: "SteamAssetDescriptions",
                column: "SteamId",
                unique: true);

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

            migrationBuilder.Sql(@"
                UPDATE[SteamAssetDescriptions] SET [Flags] = 0
            ");
        }
    }
}
