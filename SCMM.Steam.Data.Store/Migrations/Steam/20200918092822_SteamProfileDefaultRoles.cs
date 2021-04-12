using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations.Steam
{
    public partial class SteamProfileDefaultRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [SteamProfiles] SET [Roles_Serialised] = 'Administrator|VIP' WHERE [SteamId] = '76561198082101518'"); // bipolar_penguin
            migrationBuilder.Sql("UPDATE [SteamProfiles] SET [Roles_Serialised] = 'VIP' WHERE [SteamId] = '76561197983213351'"); // TGG
            migrationBuilder.Sql("UPDATE [SteamProfiles] SET [Roles_Serialised] = 'VIP' WHERE [SteamId] = '76561198523585920'"); // OMIGHTY
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
