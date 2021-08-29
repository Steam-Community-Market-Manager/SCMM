using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class ComplexTypePropertiesAreRequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE [SteamStoreItems] SET [Prices_Serialised] = '' WHERE [Prices_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "Prices_Serialised",
                table: "SteamStoreItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE [SteamStoreItemItemStore] SET [Prices_Serialised] = '' WHERE [Prices_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "Prices_Serialised",
                table: "SteamStoreItemItemStore",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE [SteamProfiles] SET [Roles_Serialised] = '' WHERE [Roles_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "Roles_Serialised",
                table: "SteamProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE [SteamProfileConfiguration] SET [List_Serialised] = '' WHERE [List_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "List_Serialised",
                table: "SteamProfileConfiguration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE [SteamItemStores] SET [Notes_Serialised] = '' WHERE [Notes_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "Notes_Serialised",
                table: "SteamItemStores",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE [SteamItemStores] SET [Media_Serialised] = '' WHERE [Media_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "Media_Serialised",
                table: "SteamItemStores",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE [SteamAssetFilter] SET [Options_Serialised] = '' WHERE [Options_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "Options_Serialised",
                table: "SteamAssetFilter",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE [SteamAssetDescriptions] SET [Tags_Serialised] = '' WHERE [Tags_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "Tags_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE [SteamAssetDescriptions] SET [Notes_Serialised] = '' WHERE [Notes_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "Notes_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE [SteamAssetDescriptions] SET [CraftingComponents_Serialised] = '' WHERE [CraftingComponents_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "CraftingComponents_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE [SteamAssetDescriptions] SET [BreaksIntoComponents_Serialised] = '' WHERE [BreaksIntoComponents_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "BreaksIntoComponents_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE [DiscordConfiguration] SET [List_Serialised] = '' WHERE [List_Serialised] IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "List_Serialised",
                table: "DiscordConfiguration",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Prices_Serialised",
                table: "SteamStoreItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Prices_Serialised",
                table: "SteamStoreItemItemStore",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Roles_Serialised",
                table: "SteamProfiles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "List_Serialised",
                table: "SteamProfileConfiguration",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes_Serialised",
                table: "SteamItemStores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Media_Serialised",
                table: "SteamItemStores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Options_Serialised",
                table: "SteamAssetFilter",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Tags_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CraftingComponents_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "BreaksIntoComponents_Serialised",
                table: "SteamAssetDescriptions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "List_Serialised",
                table: "DiscordConfiguration",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
