using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurrVet.Migrations {
    /// <inheritdoc />
    public partial class AddNotificationRedirectUrl : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<string>(
                name: "RedirectUrl",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "RedirectUrl",
                table: "Notifications");
        }
    }
}
