using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurrVet.Migrations
{
    /// <inheritdoc />
    public partial class anothergroupiddraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DraftGroupID",
                table: "AppointmentDrafts");

            migrationBuilder.AddColumn<int>(
                name: "GroupDraftId",
                table: "AppointmentDrafts",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupDraftId",
                table: "AppointmentDrafts");

            migrationBuilder.AddColumn<string>(
                name: "DraftGroupID",
                table: "AppointmentDrafts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
