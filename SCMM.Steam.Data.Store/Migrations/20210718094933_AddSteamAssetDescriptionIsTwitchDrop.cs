using Microsoft.EntityFrameworkCore.Migrations;

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamAssetDescriptionIsTwitchDrop : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTwitchDrop",
                table: "SteamAssetDescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Tag streamer commissioned twitch drops
            migrationBuilder.Sql(@"
                UPDATE [SteamAssetDescriptions] SET [IsTwitchDrop] = 1 WHERE [IsMarketable] = 0 AND [lifetimesubscriptions] = 0 AND [WorkshopFileId] IS NOT NULL AND [TimeCreated] > '2020-01-01'
            ");
            // Tag publisher twitch drops
            migrationBuilder.Sql(@"
                UPDATE [SteamAssetDescriptions] SET [IsTwitchDrop] = 1 WHERE [Description] LIKE '%Twitch%' AND [WorkshopFileId] IS NULL
            ");
            // Tag the Sofa, Industrial Door, Hobo Barrel, Ornate Tempered Revolver, and Ninja Suit items
            migrationBuilder.Sql(@"
                UPDATE [SteamAssetDescriptions] SET [IsTwitchDrop] = 1 WHERE [ClassId] IN ( '4259558303', '4259204026', '4259558302', '4277287761', '4421408844')
            ");
            // Automatically mark twitch drop inventory items as 'drops'
            migrationBuilder.Sql(@"
                UPDATE i SET i.AcquiredBy = 5
                FROM [SteamProfileInventoryItems] i
                    INNER JOIN [SteamAssetDescriptions] a ON a.Id = i.DescriptionId
                WHERE a.IsTwitchDrop = 1 AND i.AcquiredBy = 0
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTwitchDrop",
                table: "SteamAssetDescriptions");
        }
    }
}
