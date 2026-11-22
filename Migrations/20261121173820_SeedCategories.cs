using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SalefnyMVC.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "كاميرات، عدسات، وإكسسوارات تصوير", "معدات تصوير" },
                    { 2, "خيام، حقائب، ومعدات تخييم وتسلق", "معدات تخييم" },
                    { 3, "شناجير، مثاقب، وأدوات صيانة", "أدوات يدوية" },
                    { 4, "لابتوبات، تابلتات، وأجهزة صوت", "أجهزة إلكترونية" },
                    { 5, "كراسي، طاولات، ومستلزمات المناسبات", "أثاث ومناسبات" },
                    { 6, "معدات رياضية وألعاب ترفيهية", "رياضة وترفيه" },
                    { 7, "أجهزة مطبخ وأدوات قياس منزلية", "أدوات منزلية" },
                    { 8, "كتب ومواد تعليمية", "كتب ومجلات" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
