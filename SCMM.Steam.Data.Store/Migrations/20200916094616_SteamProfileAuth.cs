using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamProfileAuth : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Roles_Serialised",
                table: "SteamProfiles",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DonatorLevel",
                table: "SteamProfiles",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSignedInOn",
                table: "SteamProfiles",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AcceptedOn",
                table: "SteamAssetWorkshopFiles",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.Sql("UPDATE [SteamAssetWorkshopFiles] SET [AcceptedOn] = NULL WHERE [AcceptedOn] = '0001-01-01 00:00:00.0000000 +00:00'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Roles_Serialised",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "DonatorLevel",
                table: "SteamProfiles");

            migrationBuilder.DropColumn(
                name: "LastSignedInOn",
                table: "SteamProfiles");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AcceptedOn",
                table: "SteamAssetWorkshopFiles",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldNullable: true);
        }
    }
}
