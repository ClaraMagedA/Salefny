using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalefnyMVC.Migrations
{
    /// <inheritdoc />
    public partial class ListingApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "PhysicallyVerified",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSixClearPhotos",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUndamaged",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "QualityAccepted",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Items",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Items",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AdminNote", table: "Items");
            migrationBuilder.DropColumn(name: "ReviewedAt", table: "Items");
            migrationBuilder.DropColumn(name: "QualityAccepted", table: "Items");
            migrationBuilder.DropColumn(name: "IsUndamaged", table: "Items");
            migrationBuilder.DropColumn(name: "HasSixClearPhotos", table: "Items");
            migrationBuilder.DropColumn(name: "PhysicallyVerified", table: "Items");
            migrationBuilder.DropColumn(name: "ApprovalStatus", table: "Items");
        }
    }
}
