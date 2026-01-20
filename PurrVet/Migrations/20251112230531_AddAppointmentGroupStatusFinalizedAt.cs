using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurrVet.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentGroupStatusFinalizedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAt",
                table: "AppointmentGroups",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AppointmentGroups",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "AppointmentGroups");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AppointmentGroups");
        }
    }
}
