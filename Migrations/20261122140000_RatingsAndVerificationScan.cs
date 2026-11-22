using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalefnyMVC.Migrations
{
    /// <inheritdoc />
    public partial class RatingsAndVerificationScan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [AspNetUsers] SET [TrustRating] = 0;");

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "IdentityVerifications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NationalId");

            migrationBuilder.AddColumn<string>(
                name: "DocumentHolderName",
                table: "IdentityVerifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentHolderName",
                table: "IdentityVerifications");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "IdentityVerifications");
        }
    }
}
