using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RefactorSteamAssetDescriptionVoteData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoteRatio",
                table: "SteamAssetDescriptions");

            migrationBuilder.AddColumn<long>(
                name: "VotesDown",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VotesUp",
                table: "SteamAssetDescriptions",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VotesDown",
                table: "SteamAssetDescriptions");

            migrationBuilder.DropColumn(
                name: "VotesUp",
                table: "SteamAssetDescriptions");

            migrationBuilder.AddColumn<decimal>(
                name: "VoteRatio",
                table: "SteamAssetDescriptions",
                type: "decimal(20,20)",
                precision: 20,
                scale: 20,
                nullable: true);
        }
    }
}
