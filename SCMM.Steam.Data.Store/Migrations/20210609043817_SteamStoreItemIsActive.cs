using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class SteamStoreItemIsActive : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SteamStoreItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE [SteamStoreItems] SET [IsActive] = 0
            ");
            migrationBuilder.Sql(@"
                UPDATE s SET s.[IsActive] = 1
	                FROM [SteamStoreItems] s
	                INNER JOIN [SteamStoreItemItemStore] ss ON ss.ItemId = s.Id
	                INNER JOIN [SteamItemStores] st ON st.Id = ss.StoreId
	                WHERE st.[End] IS NULL
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SteamStoreItems");
        }
    }
}
