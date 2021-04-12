using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class AddImageDataExpiresOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresOn",
                table: "ImageData",
                type: "datetimeoffset",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresOn",
                table: "ImageData");
        }
    }
}
