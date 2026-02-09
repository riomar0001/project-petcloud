using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCloud.Migrations {
    /// <inheritdoc />
    public partial class AddSyncFields : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoSyncEnabled",
                table: "MicrosoftAccountConnections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSynced",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropColumn(
                name: "IsAutoSyncEnabled",
                table: "MicrosoftAccountConnections");

            migrationBuilder.DropColumn(
                name: "IsSynced",
                table: "Appointments");
        }
    }
}
