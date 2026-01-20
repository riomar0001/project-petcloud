using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurrVet.Migrations
{
    /// <inheritdoc />
    public partial class smscd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailSentToday",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEmailSentAt",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSmsSentAt",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderCounterDate",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmsSentToday",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailSentToday",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "LastEmailSentAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "LastSmsSentAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ReminderCounterDate",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "SmsSentToday",
                table: "Appointments");
        }
    }
}
