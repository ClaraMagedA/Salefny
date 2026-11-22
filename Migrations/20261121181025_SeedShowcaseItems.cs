using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SalefnyMVC.Migrations
{
    /// <inheritdoc />
    public partial class SeedShowcaseItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "City", "ConcurrencyStamp", "Email", "EmailConfirmed", "EscrowBalance", "FullName", "IsVerified", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TrustRating", "TwoFactorEnabled", "UserName" },
                values: new object[] { "11111111-1111-1111-1111-111111111111", 0, "القاهرة", "DEMO-OWNER-CONCURRENCY", "demo.owner@salefny.local", true, 0m, "أحمد رامي", true, true, null, "DEMO.OWNER@SALEFNY.LOCAL", "DEMO.OWNER@SALEFNY.LOCAL", null, null, false, "DEMO-OWNER-SECURITY", 5.0m, false, "demo.owner@salefny.local" });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "CategoryId", "City", "Description", "IsActive", "OwnerId", "PricePerDay", "SecurityDeposit", "Title" },
                values: new object[,]
                {
                    { 101, 1, "القاهرة", "كاميرا احترافية بحالة ممتازة مع عدسة وحقيبة.", true, "11111111-1111-1111-1111-111111111111", 850m, 5000m, "كاميرا Sony Alpha A7 III + عدسة 24-70mm" },
                    { 102, 2, "جنوب سيناء", "خيمة مقاومة للماء مع حقيبتي نوم.", true, "11111111-1111-1111-1111-111111111111", 400m, 1500m, "خيمة رحلات تتسع لـ 4 أشخاص + حقيبة نوم" },
                    { 103, 4, "الجيزة", "بروجكتور 4K مع شاشة قابلة للطي.", true, "11111111-1111-1111-1111-111111111111", 500m, 2000m, "بروجكتور ذكي بدقة 4K + شاشة عرض" },
                    { 104, 2, "الإسكندرية", "حقيبة جبلية ومجموعة عصي تسلق.", true, "11111111-1111-1111-1111-111111111111", 280m, 1000m, "حقيبة ظهر جبلية 65 لتر + عصي تسلق" },
                    { 105, 4, "القاهرة", "سماعات لاسلكية بعزل ضوضاء ممتاز.", true, "11111111-1111-1111-1111-111111111111", 150m, 800m, "سماعات Sony WH-1000XM5 بعزل الضوضاء" },
                    { 106, 1, "الجيزة", "كاميرا بتصوير 40 ميجابكسل.", true, "11111111-1111-1111-1111-111111111111", 700m, 4000m, "كاميرا Fujifilm X-T5 مع عدسة 18-55mm" },
                    { 107, 7, "الدقهلية", "ماكينة إسبريسو مع ملحقات كاملة.", true, "11111111-1111-1111-1111-111111111111", 120m, 900m, "ماكينة قهوة إسبريسو احترافية 15 بار" },
                    { 108, 4, "القاهرة", "تابلت مع قلم وحافظة.", true, "11111111-1111-1111-1111-111111111111", 350m, 2000m, "تابلت iPad Pro 11 بوصة + قلم" },
                    { 109, 5, "الإسكندرية", "10 قطع أثاث للمناسبات.", true, "11111111-1111-1111-1111-111111111111", 600m, 3000m, "مجموعة كراسي وطاولات لحفلات الزفاف" },
                    { 110, 1, "الجيزة", "درون بكاميرا 4K وحقيبة سفر.", true, "11111111-1111-1111-1111-111111111111", 900m, 6000m, "درون DJI Air 3 بكاميرا 4K" },
                    { 111, 6, "القاهرة", "دراجة جبلية بحالة ممتازة.", true, "11111111-1111-1111-1111-111111111111", 180m, 1200m, "دراجة هوائية جبلية 27.5 بوصة + خوذة" },
                    { 112, 7, "الدقهلية", "طقم أدوات قياس منزلية.", true, "11111111-1111-1111-1111-111111111111", 60m, 400m, "ميزان حراري رقمي وأدوات قياس منزلية" },
                    { 113, 3, "القاهرة", "مثقاب لاسلكي مع طقم ريش.", true, "11111111-1111-1111-1111-111111111111", 90m, 700m, "مثقاب كهربائي 18 فولط مع طقم ريش" },
                    { 114, 2, "الجيزة", "خيمة خفيفة وسهلة الحمل.", true, "11111111-1111-1111-1111-111111111111", 220m, 1000m, "خيمة خفيفة لشخصين مقاومة للماء" },
                    { 115, 4, "الإسكندرية", "لابتوب مع شاحن origional.", true, "11111111-1111-1111-1111-111111111111", 750m, 4000m, "لابتوب MacBook Air M2 13 بوصة" },
                    { 116, 5, "الدقهلية", "طاولة و6 كراسي للمناسبات.", true, "11111111-1111-1111-1111-111111111111", 350m, 1800m, "طاولة طعام قابلة للطي + 6 كراسي" },
                    { 117, 7, "القاهرة", "شواية كهربائية واسعة.", true, "11111111-1111-1111-1111-111111111111", 140m, 800m, "شواية كهربائية كبيرة مع ملحقاتها" },
                    { 118, 7, "الجيزة", "مكنسة بخار للمنزل والسيارة.", true, "11111111-1111-1111-1111-111111111111", 110m, 900m, "مكنسة تنظيف بالبخار متعددة الاستخدامات" },
                    { 119, 6, "الإسكندرية", "قارب ترفيهي مع مجاديف.", true, "11111111-1111-1111-1111-111111111111", 1200m, 8000m, "قارب صغير للترفيه مع مجاديف" },
                    { 120, 8, "القاهرة", "مجموعة كتب تعليمية.", true, "11111111-1111-1111-1111-111111111111", 45m, 300m, "مجموعة كتب ومواد تعليمية مختارة" }
                });

            migrationBuilder.InsertData(
                table: "ItemImages",
                columns: new[] { "Id", "AngleIndex", "ImagePath", "ItemId" },
                values: new object[,]
                {
                    { 101, 1, "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?auto=format&fit=crop&w=600&q=80", 101 },
                    { 102, 1, "https://images.unsplash.com/photo-1504280390367-361c6d9f38f4?auto=format&fit=crop&w=600&q=80", 102 },
                    { 103, 1, "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?auto=format&fit=crop&w=600&q=80", 103 },
                    { 104, 1, "https://images.unsplash.com/photo-1533090161767-e6ffed986c88?auto=format&fit=crop&w=600&q=80", 104 },
                    { 105, 1, "https://images.unsplash.com/photo-1583394838336-acd977736f90?auto=format&fit=crop&w=600&q=80", 105 },
                    { 106, 1, "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?auto=format&fit=crop&w=600&q=80", 106 },
                    { 107, 1, "https://images.unsplash.com/photo-1572569511254-d8f925fe2cbb?auto=format&fit=crop&w=600&q=80", 107 },
                    { 108, 1, "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?auto=format&fit=crop&w=600&q=80", 108 },
                    { 109, 1, "https://images.unsplash.com/photo-1503602642458-232111445657?auto=format&fit=crop&w=600&q=80", 109 },
                    { 110, 1, "https://images.unsplash.com/photo-1498049794561-7780e7231661?auto=format&fit=crop&w=600&q=80", 110 },
                    { 111, 1, "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?auto=format&fit=crop&w=600&q=80", 111 },
                    { 112, 1, "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=600&q=80", 112 },
                    { 113, 1, "https://images.unsplash.com/photo-1504148455328-c376907d081c?auto=format&fit=crop&w=600&q=80", 113 },
                    { 114, 1, "https://images.unsplash.com/photo-1478131143081-80f7f84ca84d?auto=format&fit=crop&w=600&q=80", 114 },
                    { 115, 1, "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=600&q=80", 115 },
                    { 116, 1, "https://images.unsplash.com/photo-1499933374294-4584851497cc?auto=format&fit=crop&w=600&q=80", 116 },
                    { 117, 1, "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=600&q=80", 117 },
                    { 118, 1, "https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=600&q=80", 118 },
                    { 119, 1, "https://images.unsplash.com/photo-1567899378494-47b22a2ae96a?auto=format&fit=crop&w=600&q=80", 119 },
                    { 120, 1, "https://images.unsplash.com/photo-1481627834876-b7833e8f5570?auto=format&fit=crop&w=600&q=80", 120 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "ItemImages",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 119);

            migrationBuilder.DeleteData(
                table: "Items",
                keyColumn: "Id",
                keyValue: 120);

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "11111111-1111-1111-1111-111111111111");
        }
    }
}
