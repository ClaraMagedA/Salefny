using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalefnyMVC.Migrations
{
    /// <inheritdoc />
    public partial class OwnerRequestProductSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableFrom",
                table: "OwnerRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AvailableNow",
                table: "OwnerRequests",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableTo",
                table: "OwnerRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "OwnerRequests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "OwnerRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CreatedItemId",
                table: "OwnerRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "OwnerRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerDay",
                table: "OwnerRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductTitle",
                table: "OwnerRequests",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SecurityDeposit",
                table: "OwnerRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "OwnerRequestImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AngleIndex = table.Column<int>(type: "int", nullable: false),
                    OwnerRequestId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerRequestImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnerRequestImages_OwnerRequests_OwnerRequestId",
                        column: x => x.OwnerRequestId,
                        principalTable: "OwnerRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerRequests_CategoryId",
                table: "OwnerRequests",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerRequestImages_OwnerRequestId",
                table: "OwnerRequestImages",
                column: "OwnerRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_OwnerRequests_Categories_CategoryId",
                table: "OwnerRequests",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OwnerRequests_Categories_CategoryId",
                table: "OwnerRequests");

            migrationBuilder.DropTable(
                name: "OwnerRequestImages");

            migrationBuilder.DropIndex(
                name: "IX_OwnerRequests_CategoryId",
                table: "OwnerRequests");

            migrationBuilder.DropColumn(name: "AvailableFrom", table: "OwnerRequests");
            migrationBuilder.DropColumn(name: "AvailableNow", table: "OwnerRequests");
            migrationBuilder.DropColumn(name: "AvailableTo", table: "OwnerRequests");
            migrationBuilder.DropColumn(name: "CategoryId", table: "OwnerRequests");
            migrationBuilder.DropColumn(name: "City", table: "OwnerRequests");
            migrationBuilder.DropColumn(name: "CreatedItemId", table: "OwnerRequests");
            migrationBuilder.DropColumn(name: "Description", table: "OwnerRequests");
            migrationBuilder.DropColumn(name: "PricePerDay", table: "OwnerRequests");
            migrationBuilder.DropColumn(name: "ProductTitle", table: "OwnerRequests");
            migrationBuilder.DropColumn(name: "SecurityDeposit", table: "OwnerRequests");
        }
    }
}
