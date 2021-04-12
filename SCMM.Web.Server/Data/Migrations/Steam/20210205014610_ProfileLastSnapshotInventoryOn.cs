﻿using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SCMM.Web.Server.Data.Migrations.Steam
{
    public partial class ProfileLastSnapshotInventoryOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSnapshotInventoryOn",
                table: "SteamProfiles",
                type: "datetimeoffset",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSnapshotInventoryOn",
                table: "SteamProfiles");
        }
    }
}