using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCloud.Migrations {
    /// <inheritdoc />
    public partial class DueDate : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Appointments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Appointments");
        }
    }
}
