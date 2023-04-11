using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    /// <inheritdoc />
    public partial class AddSteamItemDefinitionArchiveSizeAndItemCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemDefinitionsCount",
                table: "SteamItemDefinitionsArchive",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ItemDefinitionsSize",
                table: "SteamItemDefinitionsArchive",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemDefinitionsCount",
                table: "SteamItemDefinitionsArchive");

            migrationBuilder.DropColumn(
                name: "ItemDefinitionsSize",
                table: "SteamItemDefinitionsArchive");
        }
    }
}
