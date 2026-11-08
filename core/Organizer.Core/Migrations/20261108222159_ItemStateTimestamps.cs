using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Organizer.Core.Migrations
{
    /// <inheritdoc />
    public partial class ItemStateTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DoneAt",
                table: "Items",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DoneAt",
                table: "Items");
        }
    }
}
