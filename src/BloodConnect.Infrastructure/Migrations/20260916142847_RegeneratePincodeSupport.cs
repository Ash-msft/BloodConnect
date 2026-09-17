using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegeneratePincodeSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Pincode",
                table: "DonorProfiles",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pincode",
                table: "BloodRequests",
                type: "TEXT",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pincode",
                table: "DonorProfiles");

            migrationBuilder.DropColumn(
                name: "Pincode",
                table: "BloodRequests");
        }
    }
}
