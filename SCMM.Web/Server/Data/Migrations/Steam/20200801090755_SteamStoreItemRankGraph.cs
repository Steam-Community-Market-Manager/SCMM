using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class SteamStoreItemRankGraph : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoreRankGraph_Serialised",
                table: "SteamStoreItems",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreRankGraph_Serialised",
                table: "SteamStoreItems");
        }
    }
}
