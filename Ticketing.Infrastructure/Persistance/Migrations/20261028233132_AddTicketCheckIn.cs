using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Tickets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Tickets");
        }
    }
}
