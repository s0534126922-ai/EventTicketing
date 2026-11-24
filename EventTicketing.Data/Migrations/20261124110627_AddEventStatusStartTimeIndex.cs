using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketing.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventStatusStartTimeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Events_Status_StartTime",
                table: "Events",
                columns: new[] { "Status", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_Status_StartTime",
                table: "Events");
        }
    }
}
