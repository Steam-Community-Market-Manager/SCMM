using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class RefactorSteamAssetDescriptionFavouritedAndSubscriptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LifetimeSubscriptions",
                table: "SteamAssetDescriptions",
                newName: "SubscriptionsLifetime");

            migrationBuilder.RenameColumn(
                name: "CurrentSubscriptions",
                table: "SteamAssetDescriptions",
                newName: "SubscriptionsCurrent");

            migrationBuilder.RenameColumn(
                name: "LifetimeFavourited",
                table: "SteamAssetDescriptions",
                newName: "FavouritedLifetime");

            migrationBuilder.RenameColumn(
                name: "CurrentFavourited",
                table: "SteamAssetDescriptions",
                newName: "FavouritedCurrent");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubscriptionsLifetime",
                table: "SteamAssetDescriptions",
                newName: "LifetimeSubscriptions");

            migrationBuilder.RenameColumn(
                name: "FavouritedLifetime",
                table: "SteamAssetDescriptions",
                newName: "LifetimeFavourited");

            migrationBuilder.RenameColumn(
                name: "SubscriptionsCurrent",
                table: "SteamAssetDescriptions",
                newName: "CurrentSubscriptions");

            migrationBuilder.RenameColumn(
                name: "FavouritedCurrent",
                table: "SteamAssetDescriptions",
                newName: "CurrentFavourited");
        }
    }
}
