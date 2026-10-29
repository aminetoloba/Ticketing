using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class FilterStaffAssignmentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StaffAssignments_StaffId_EventId",
                table: "StaffAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignments_StaffId_EventId",
                table: "StaffAssignments",
                columns: new[] { "StaffId", "EventId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StaffAssignments_StaffId_EventId",
                table: "StaffAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignments_StaffId_EventId",
                table: "StaffAssignments",
                columns: new[] { "StaffId", "EventId" },
                unique: true);
        }
    }
}
