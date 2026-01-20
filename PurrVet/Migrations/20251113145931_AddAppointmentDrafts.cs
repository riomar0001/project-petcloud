using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurrVet.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDrafts_CategoryID",
                table: "AppointmentDrafts",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDrafts_PetID",
                table: "AppointmentDrafts",
                column: "PetID");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDrafts_SubtypeID",
                table: "AppointmentDrafts",
                column: "SubtypeID");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentDrafts_Pets_PetID",
                table: "AppointmentDrafts",
                column: "PetID",
                principalTable: "Pets",
                principalColumn: "PetID");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentDrafts_ServiceCategories_CategoryID",
                table: "AppointmentDrafts",
                column: "CategoryID",
                principalTable: "ServiceCategories",
                principalColumn: "CategoryID");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentDrafts_ServiceSubtypes_SubtypeID",
                table: "AppointmentDrafts",
                column: "SubtypeID",
                principalTable: "ServiceSubtypes",
                principalColumn: "SubtypeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentDrafts_Pets_PetID",
                table: "AppointmentDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentDrafts_ServiceCategories_CategoryID",
                table: "AppointmentDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentDrafts_ServiceSubtypes_SubtypeID",
                table: "AppointmentDrafts");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentDrafts_CategoryID",
                table: "AppointmentDrafts");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentDrafts_PetID",
                table: "AppointmentDrafts");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentDrafts_SubtypeID",
                table: "AppointmentDrafts");
        }
    }
}
