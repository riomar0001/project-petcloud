using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCloud.Migrations
{
    /// <inheritdoc />
    public partial class CleanupPetID1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Pets_PetID",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Pets_PetID1",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_PetID1",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PetID1",
                table: "Appointments");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Pets_PetID",
                table: "Appointments",
                column: "PetID",
                principalTable: "Pets",
                principalColumn: "PetID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Pets_PetID",
                table: "Appointments");

            migrationBuilder.AddColumn<int>(
                name: "PetID1",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PetID1",
                table: "Appointments",
                column: "PetID1");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Pets_PetID",
                table: "Appointments",
                column: "PetID",
                principalTable: "Pets",
                principalColumn: "PetID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Pets_PetID1",
                table: "Appointments",
                column: "PetID1",
                principalTable: "Pets",
                principalColumn: "PetID");
        }
    }
}
