using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurrVet.Migrations
{
    /// <inheritdoc />
    public partial class _2falocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastLoginDevice",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastLoginIP",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLoginDevice",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginIP",
                table: "Users");
        }
    }
}
