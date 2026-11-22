using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;
using System.Reflection.Emit;

namespace SalefnyMVC.Data
{
    public class SalefnyDbContext : IdentityDbContext<ApplicationUser>
    {
        public SalefnyDbContext(DbContextOptions<SalefnyDbContext> options) : base(options)
        {
        }

        public DbSet<Item> Items { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ItemImage> ItemImages { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingInspection> BookingInspections { get; set; }
        public DbSet<IdentityVerification> IdentityVerifications { get; set; }
        public DbSet<DisputeLog> DisputeLogs { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<FavoriteItem> FavoriteItems { get; set; }
        public DbSet<AvailabilityBlock> AvailabilityBlocks { get; set; }
        public DbSet<OwnerRequest> OwnerRequests { get; set; }
        public DbSet<OwnerRequestImage> OwnerRequestImages { get; set; }
        public DbSet<EscrowTransaction> EscrowTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Seed the categories used by the marketplace filter and the listing form.
            builder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "معدات تصوير", Description = "كاميرات، عدسات، وإكسسوارات تصوير" },
                new Category { Id = 2, Name = "معدات تخييم", Description = "خيام، حقائب، ومعدات تخييم وتسلق" },
                new Category { Id = 3, Name = "أدوات يدوية", Description = "شناجير، مثاقب، وأدوات صيانة" },
                new Category { Id = 4, Name = "أجهزة إلكترونية", Description = "لابتوبات، تابلتات، وأجهزة صوت" },
                new Category { Id = 5, Name = "أثاث ومناسبات", Description = "كراسي، طاولات، ومستلزمات المناسبات" },
                new Category { Id = 6, Name = "رياضة وترفيه", Description = "معدات رياضية وألعاب ترفيهية" },
                new Category { Id = 7, Name = "أدوات منزلية", Description = "أجهزة مطبخ وأدوات قياس منزلية" },
                new Category { Id = 8, Name = "كتب ومجلات", Description = "كتب ومواد تعليمية" }
            );

            // One verified demo owner so the showcase items have a valid FK.
            const string demoOwnerId = "11111111-1111-1111-1111-111111111111";
            builder.Entity<ApplicationUser>().HasData(new ApplicationUser
            {
                Id = demoOwnerId,
                UserName = "demo.owner@salefny.local",
                NormalizedUserName = "DEMO.OWNER@SALEFNY.LOCAL",
                Email = "demo.owner@salefny.local",
                NormalizedEmail = "DEMO.OWNER@SALEFNY.LOCAL",
                EmailConfirmed = true,
                SecurityStamp = "DEMO-OWNER-SECURITY",
                ConcurrencyStamp = "DEMO-OWNER-CONCURRENCY",
                LockoutEnabled = true,
                AccessFailedCount = 0,
                FullName = "أحمد رامي",
                City = "القاهرة",
                IsVerified = true,
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                TrustRating = 0.0m,
                AverageRating = 0.0m,
                EscrowBalance = 0m
            });

            // The 20 items that used to be hardcoded on the home page now live in the DB,
            // so home, browse, details, search and bookings all use the same catalogue.
            var demoItems = new[]
            {
                new Item { Id = 101,  Title = "كاميرا Sony Alpha A7 III + عدسة 24-70mm", CategoryId = 1, PricePerDay = 850m,   SecurityDeposit = 5000m, City = "القاهرة",     Description = "كاميرا احترافية بحالة ممتازة مع عدسة وحقيبة.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 102,  Title = "خيمة رحلات تتسع لـ 4 أشخاص + حقيبة نوم", CategoryId = 2, PricePerDay = 400m,   SecurityDeposit = 1500m, City = "جنوب سيناء",  Description = "خيمة مقاومة للماء مع حقيبتي نوم.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 103,  Title = "بروجكتور ذكي بدقة 4K + شاشة عرض", CategoryId = 4, PricePerDay = 500m,   SecurityDeposit = 2000m, City = "الجيزة",       Description = "بروجكتور 4K مع شاشة قابلة للطي.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 104,  Title = "حقيبة ظهر جبلية 65 لتر + عصي تسلق", CategoryId = 2, PricePerDay = 280m,   SecurityDeposit = 1000m, City = "الإسكندرية",   Description = "حقيبة جبلية ومجموعة عصي تسلق.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 105,  Title = "سماعات Sony WH-1000XM5 بعزل الضوضاء", CategoryId = 4, PricePerDay = 150m,   SecurityDeposit = 800m,  City = "القاهرة",     Description = "سماعات لاسلكية بعزل ضوضاء ممتاز.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 106,  Title = "كاميرا Fujifilm X-T5 مع عدسة 18-55mm", CategoryId = 1, PricePerDay = 700m,   SecurityDeposit = 4000m, City = "الجيزة",       Description = "كاميرا بتصوير 40 ميجابكسل.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 107,  Title = "ماكينة قهوة إسبريسو احترافية 15 بار", CategoryId = 7, PricePerDay = 120m,   SecurityDeposit = 900m,  City = "الدقهلية",    Description = "ماكينة إسبريسو مع ملحقات كاملة.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 108,  Title = "تابلت iPad Pro 11 بوصة + قلم", CategoryId = 4, PricePerDay = 350m,   SecurityDeposit = 2000m, City = "القاهرة",     Description = "تابلت مع قلم وحافظة.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 109,  Title = "مجموعة كراسي وطاولات لحفلات الزفاف", CategoryId = 5, PricePerDay = 600m,   SecurityDeposit = 3000m, City = "الإسكندرية",   Description = "10 قطع أثاث للمناسبات.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 110, Title = "درون DJI Air 3 بكاميرا 4K", CategoryId = 1, PricePerDay = 900m,   SecurityDeposit = 6000m, City = "الجيزة",       Description = "درون بكاميرا 4K وحقيبة سفر.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 111, Title = "دراجة هوائية جبلية 27.5 بوصة + خوذة", CategoryId = 6, PricePerDay = 180m,   SecurityDeposit = 1200m, City = "القاهرة",     Description = "دراجة جبلية بحالة ممتازة.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 112, Title = "ميزان حراري رقمي وأدوات قياس منزلية", CategoryId = 7, PricePerDay = 60m,    SecurityDeposit = 400m,  City = "الدقهلية",    Description = "طقم أدوات قياس منزلية.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 113, Title = "مثقاب كهربائي 18 فولط مع طقم ريش", CategoryId = 3, PricePerDay = 90m,    SecurityDeposit = 700m,  City = "القاهرة",     Description = "مثقاب لاسلكي مع طقم ريش.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 114, Title = "خيمة خفيفة لشخصين مقاومة للماء", CategoryId = 2, PricePerDay = 220m,   SecurityDeposit = 1000m, City = "الجيزة",       Description = "خيمة خفيفة وسهلة الحمل.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 115, Title = "لابتوب MacBook Air M2 13 بوصة", CategoryId = 4, PricePerDay = 750m,   SecurityDeposit = 4000m, City = "الإسكندرية",   Description = "لابتوب مع شاحن origional.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 116, Title = "طاولة طعام قابلة للطي + 6 كراسي", CategoryId = 5, PricePerDay = 350m,   SecurityDeposit = 1800m, City = "الدقهلية",    Description = "طاولة و6 كراسي للمناسبات.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 117, Title = "شواية كهربائية كبيرة مع ملحقاتها", CategoryId = 7, PricePerDay = 140m,   SecurityDeposit = 800m,  City = "القاهرة",     Description = "شواية كهربائية واسعة.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 118, Title = "مكنسة تنظيف بالبخار متعددة الاستخدامات", CategoryId = 7, PricePerDay = 110m, SecurityDeposit = 900m,  City = "الجيزة",       Description = "مكنسة بخار للمنزل والسيارة.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 119, Title = "قارب صغير للترفيه مع مجاديف", CategoryId = 6, PricePerDay = 1200m,  SecurityDeposit = 8000m, City = "الإسكندرية",   Description = "قارب ترفيهي مع مجاديف.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved },
                new Item { Id = 120, Title = "مجموعة كتب ومواد تعليمية مختارة", CategoryId = 8, PricePerDay = 45m,    SecurityDeposit = 300m,  City = "القاهرة",     Description = "مجموعة كتب تعليمية.", OwnerId = demoOwnerId, IsActive = true, ApprovalStatus = ListingApprovalStatus.Approved }
            };
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (var demoItem in demoItems) demoItem.CreatedAt = seedDate;
            builder.Entity<Item>().HasData(demoItems);

            var demoImageUrls = new[]
            {
                "1516035069371-29a1b244cc32", "1504280390367-361c6d9f38f4", "1517694712202-14dd9538aa97",
                "1533090161767-e6ffed986c88", "1583394838336-acd977736f90", "1526170375885-4d8ecf77b99f",
                "1572569511254-d8f925fe2cbb", "1544244015-0df4b3ffc6b0", "1503602642458-232111445657",
                "1498049794561-7780e7231661", "1558618666-fcd25c85cd64", "1523275335684-37898b6baf30",
                "1504148455328-c376907d081c", "1478131143081-80f7f84ca84d", "1517336714731-489689fd1ca8",
                "1499933374294-4584851497cc", "1555939594-58d7cb561ad1", "1581578731548-c64695cc6952",
                "1567899378494-47b22a2ae96a", "1481627834876-b7833e8f5570"
            };
            builder.Entity<ItemImage>().HasData(demoItems.Select(i => new ItemImage
            {
                Id = i.Id,
                ItemId = i.Id,
                AngleIndex = 1,
                ImagePath = $"https://images.unsplash.com/photo-{demoImageUrls[i.Id - 101]}?auto=format&fit=crop&w=600&q=80"
            }).ToArray());

            // Configure decimal precisions for Egyptian Pound (EGP) financial values
            builder.Entity<Item>()
                .Property(i => i.PricePerDay)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Item>()
                .Property(i => i.SecurityDeposit)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Item>()
                .Property(i => i.Latitude)
                .HasColumnType("decimal(9,6)");

            builder.Entity<Item>()
                .Property(i => i.Longitude)
                .HasColumnType("decimal(9,6)");

            builder.Entity<Booking>()
                .Property(b => b.TotalRentalAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Booking>()
                .Property(b => b.EscrowDepositAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<OwnerRequest>()
                .Property(r => r.PricePerDay)
                .HasColumnType("decimal(18,2)");

            builder.Entity<OwnerRequest>()
                .Property(r => r.SecurityDeposit)
                .HasColumnType("decimal(18,2)");

            builder.Entity<ApplicationUser>()
                .Property(u => u.TrustRating)
                .HasColumnType("decimal(18,2)");

            builder.Entity<ApplicationUser>()
                .Property(u => u.EscrowBalance)
                .HasColumnType("decimal(18,2)");

            builder.Entity<ApplicationUser>()
                .Property(u => u.AverageRating)
                .HasColumnType("decimal(18,2)");

            // Indexes used by the documented search, booking and dashboard queries.
            builder.Entity<Item>()
                .HasIndex(i => new { i.CategoryId, i.City, i.IsActive });
            builder.Entity<Item>()
                .HasIndex(i => i.OwnerId);
            builder.Entity<Booking>()
                .HasIndex(b => new { b.ItemId, b.StartDate, b.EndDate });
            builder.Entity<Booking>()
                .HasIndex(b => b.RenterId);
            builder.Entity<Booking>()
                .HasIndex(b => b.Status);
            builder.Entity<Review>()
                .HasIndex(r => new { r.BookingId, r.ReviewerId })
                .IsUnique();
            builder.Entity<Review>()
                .HasIndex(r => r.RevieweeId);
            builder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            builder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<EscrowTransaction>()
                .HasOne(e => e.Booking)
                .WithMany(b => b.EscrowTransactions)
                .HasForeignKey(e => e.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EscrowTransaction>()
                .Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            // SQL Server rejects multiple cascade paths. Deleting an Item must not
            // cascade into these dependent tables because they are also reachable
            // through User -> Bookings.
            builder.Entity<Booking>()
                .HasOne(b => b.Item)
                .WithMany(i => i.Bookings)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Item)
                .WithMany(i => i.Reviews)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Reviewee)
                .WithMany()
                .HasForeignKey(r => r.RevieweeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FavoriteItem>()
                .HasOne(f => f.Item)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OwnerRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OwnerRequest>()
                .Property(r => r.AdminNote)
                .HasMaxLength(500);
        }
    }
}