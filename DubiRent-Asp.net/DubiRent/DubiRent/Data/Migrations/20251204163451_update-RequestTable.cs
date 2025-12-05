using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DubiRent.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateRequestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ViewingRequests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "ViewingRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "ViewingRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "ViewingRequests");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "ViewingRequests");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "ViewingRequests");
        }
    }
}
