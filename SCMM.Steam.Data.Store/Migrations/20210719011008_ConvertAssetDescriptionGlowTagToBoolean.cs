using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class ConvertAssetDescriptionGlowTagToBoolean : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE [SteamAssetDescriptions] SET [Tags_Serialised] = REPLACE([Tags_Serialised], 'glow=Glow', 'glow=true') WHERE [Tags_Serialised] LIKE '%glow=Glow%'
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE [SteamAssetDescriptions] SET [Tags_Serialised] = REPLACE([Tags_Serialised], 'glow=true', 'glow=Glow') WHERE [Tags_Serialised] LIKE '%glow=true%'
            ");
        }
    }
}
