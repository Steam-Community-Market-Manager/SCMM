using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class ConvertModeratorRoleToContributorRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE [SteamProfiles] SET [Roles_Serialised] = REPLACE([Roles_Serialised], 'Moderator', 'Contributor') WHERE [Roles_Serialised] LIKE '%Moderator%'
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE [SteamProfiles] SET [Roles_Serialised] = REPLACE([Roles_Serialised], 'Contributor', 'Moderator') WHERE [Roles_Serialised] LIKE '%Contributor%'
            ");
        }
    }
}
