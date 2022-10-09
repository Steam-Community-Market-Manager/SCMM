using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCMM.Steam.Data.Store.Migrations
{
    public partial class AddSteamWorkshopFile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SteamWorkshopFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatorId = table.Column<decimal>(type: "decimal(20,0)", nullable: true),
                    CreatorProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemCollection = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags_Serialised = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviewUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Previews_Serialised = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubscriptionsCurrent = table.Column<long>(type: "bigint", nullable: true),
                    SubscriptionsLifetime = table.Column<long>(type: "bigint", nullable: true),
                    FavouritedCurrent = table.Column<long>(type: "bigint", nullable: true),
                    FavouritedLifetime = table.Column<long>(type: "bigint", nullable: true),
                    Views = table.Column<long>(type: "bigint", nullable: true),
                    VotesUp = table.Column<long>(type: "bigint", nullable: true),
                    VotesDown = table.Column<long>(type: "bigint", nullable: true),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    TimeAccepted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TimeUpdated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TimeCreated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TimeRefreshed = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SteamId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AppId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamWorkshopFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SteamWorkshopFiles_SteamApps_AppId",
                        column: x => x.AppId,
                        principalTable: "SteamApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SteamWorkshopFiles_SteamProfiles_CreatorProfileId",
                        column: x => x.CreatorProfileId,
                        principalTable: "SteamProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SteamWorkshopFiles_AppId",
                table: "SteamWorkshopFiles",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamWorkshopFiles_CreatorProfileId",
                table: "SteamWorkshopFiles",
                column: "CreatorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SteamWorkshopFiles_SteamId",
                table: "SteamWorkshopFiles",
                column: "SteamId",
                unique: true,
                filter: "[SteamId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SteamWorkshopFiles");
        }
    }
}
