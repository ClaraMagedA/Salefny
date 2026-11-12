using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;
using SalefnyMVC.ViewModels;

namespace SalefnyMVC.Services;

public class BookingService : IBookingService
{
    private const decimal PlatformFeeRate = 0.10m;   // 10% of the rental amount
    private const int MaxRentalDays = 90;
    private const int MaxAdvanceDays = 365;

    private const string NotFoundOrDenied = "الحجز غير موجود أو لا تملك صلاحية عليه.";
    private const string TryAgain = "تعذر تنفيذ العملية الآن، حاول مرة أخرى.";
    private const string Conflict = "تم تعديل هذا الحجز من طرف آخر، أعد تحميل الصفحة وحاول مرة أخرى.";

    private static readonly BookingStatus[] BlockingStatuses = { BookingStatus.Approved, BookingStatus.Active };
    private static readonly BookingStatus[] OpenStatuses = { BookingStatus.Pending, BookingStatus.Approved, BookingStatus.Active };

    private readonly SalefnyDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<BookingService> _logger;

    public BookingService(SalefnyDbContext db, IEmailService email, ILogger<BookingService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    // ───────────────────────── Pricing & availability ─────────────────────────

    public async Task<BookingPriceBreakdown?> CalculatePriceAsync(
        int itemId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        if (end.Date <= start.Date) return null;
        var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId, ct);
        return item is null ? null : Calculate(item, start, end);
    }

    public async Task<bool> IsAvailableAsync(
        int itemId, DateTime start, DateTime end, int? excludeBookingId = null, CancellationToken ct = default)
    {
        if (end.Date <= start.Date) return false;

        // Half-open interval overlap: [start, end) intersects [b.Start, b.End)
        var bookingOverlap = await _db.Bookings.AsNoTracking().AnyAsync(b =>
            b.ItemId == itemId &&
            b.Id != excludeBookingId &&
            BlockingStatuses.Contains(b.Status) &&
            b.StartDate < end && b.EndDate > start, ct);
        if (bookingOverlap) return false;

        var blockOverlap = await _db.AvailabilityBlocks.AsNoTracking().AnyAsync(a =>
            a.ItemId == itemId && a.StartDate < end && a.EndDate > start, ct);
        return !blockOverlap;
    }

    // ───────────────────────── Commands ─────────────────────────

    public async Task<ServiceResult<Booking>> CreateAsync(
        BookingRequestViewModel model, string renterId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (string.IsNullOrWhiteSpace(renterId))
            return ServiceResult<Booking>.Fail("المستخدم غير معروف.");

        var today = DateTime.UtcNow.Date;
        var start = model.StartDate.Date;
        var end = model.EndDate.Date;

        if (start < today)
            return ServiceResult<Booking>.Fail("لا يمكن الحجز في تاريخ سابق.");
        if (end <= start)
            return ServiceResult<Booking>.Fail("تاريخ الإرجاع يجب أن يكون بعد تاريخ البدء.");
        if ((end - start).Days > MaxRentalDays)
            return ServiceResult<Booking>.Fail($"أقصى مدة للإيجار {MaxRentalDays} يوماً.");
        if ((start - today).Days > MaxAdvanceDays)
            return ServiceResult<Booking>.Fail("لا يمكن الحجز قبل أكثر من سنة.");

        try
        {
            // Availability check + insert are one serializable transaction => zero double bookings.
            var outcome = await RunAtomicAsync(async () =>
            {
                var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == model.ItemId, ct);
                if (item is null || !item.IsActive)
                    return Outcome.Fail("هذا العنصر غير متاح للإيجار.");
                if (item.OwnerId == renterId)
                    return Outcome.Fail("لا يمكنك حجز عنصر تملكه.");

                var renterVerified = await _db.Users.AsNoTracking()
                    .Where(u => u.Id == renterId)
                    .Select(u => (bool?)u.IsVerified)
                    .FirstOrDefaultAsync(ct);
                if (renterVerified is null)
                    return Outcome.Fail("المستخدم غير موجود.");
                if (renterVerified == false)
                    return Outcome.Fail("يجب توثيق الهوية قبل إنشاء أي حجز.");

                if (!await IsAvailableAsync(item.Id, start, end, null, ct))
                    return Outcome.Fail("العنصر غير متاح في الفترة المحددة.");

                var duplicate = await _db.Bookings.AsNoTracking().AnyAsync(b =>
                    b.ItemId == item.Id && b.RenterId == renterId &&
                    OpenStatuses.Contains(b.Status) &&
                    b.StartDate < end && b.EndDate > start, ct);
                if (duplicate)
                    return Outcome.Fail("لديك طلب حجز قائم لهذا العنصر في فترة متداخلة.");

                // Prices are always recomputed server-side. The posted view model is never trusted.
                var price = Calculate(item, start, end);

                var booking = new Booking
                {
                    ItemId = item.Id,
                    RenterId = renterId,
                    StartDate = start,
                    EndDate = end,
                    TotalRentalAmount = price.RentalAmount,
                    EscrowDepositAmount = price.DepositAmount,
                    Status = BookingStatus.Pending
                };
                _db.Bookings.Add(booking);

                var text = TextUtil.Clean(
                    $"لديك طلب حجز جديد على \"{item.Title}\" من {Fmt(start)} إلى {Fmt(end)}.", 255);
                _db.Notifications.Add(new Notification { UserId = item.OwnerId, Message = text });

                await _db.SaveChangesAsync(ct);

                var ownerEmail = await _db.Users.AsNoTracking()
                    .Where(u => u.Id == item.OwnerId).Select(u => u.Email).FirstOrDefaultAsync(ct);

                return new Outcome(ServiceResult<Booking>.Ok(booking), ownerEmail, "طلب حجز جديد", text);
            }, ct);

            await NotifyByEmailAsync(outcome, ct);
            return outcome.Result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while creating booking for item {ItemId}", model.ItemId);
            return ServiceResult<Booking>.Fail(Conflict);
        }
        catch (Exception ex) when (ex is DbUpdateException or System.Data.Common.DbException)
        {
            _logger.LogError(ex, "Booking creation failed for renter {RenterId}, item {ItemId}", renterId, model.ItemId);
            return ServiceResult<Booking>.Fail(TryAgain);
        }
    }

    public Task<ServiceResult<Booking>> ApproveAsync(int bookingId, string ownerId, CancellationToken ct = default) =>
        TransitionAsync(bookingId, ownerId, ApproveRule, ct);

    public Task<ServiceResult<Booking>> RejectAsync(int bookingId, string ownerId, CancellationToken ct = default) =>
        TransitionAsync(bookingId, ownerId, RejectRule, ct);

    public Task<ServiceResult<Booking>> CancelAsync(int bookingId, string userId, CancellationToken ct = default) =>
        TransitionAsync(bookingId, userId, CancelRule, ct);

    public Task<ServiceResult<Booking>> MarkActiveAsync(int bookingId, string ownerId, CancellationToken ct = default) =>
        TransitionAsync(bookingId, ownerId, ActivateRule, ct);

    public Task<ServiceResult<Booking>> CompleteAsync(int bookingId, string ownerId, CancellationToken ct = default) =>
        TransitionAsync(bookingId, ownerId, CompleteRule, ct);

    // ───────────────────────── Queries ─────────────────────────

    public async Task<IReadOnlyList<Booking>> GetRenterBookingsAsync(string renterId, CancellationToken ct = default) =>
        await _db.Bookings.AsNoTracking()
            .Include(b => b.Item)
            .Where(b => b.RenterId == renterId)
            .OrderByDescending(b => b.StartDate)
            .Take(500)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Booking>> GetOwnerRequestsAsync(string ownerId, CancellationToken ct = default) =>
        await _db.Bookings.AsNoTracking()
            .Include(b => b.Item)
            .Include(b => b.Renter)
            .Where(b => b.Item.OwnerId == ownerId)
            .OrderByDescending(b => b.StartDate)
            .Take(500)
            .ToListAsync(ct);

    // ───────────────────────── State machine ─────────────────────────

    private sealed record Rule(
        BookingStatus[] From,
        BookingStatus To,
        Func<Booking, string, bool> IsAllowedActor,
        Func<Booking, string, ApplicationUser> Recipient,
        Func<Booking, string> Message,
        string Subject,
        bool RecheckAvailability = false);

    private static bool IsOwner(Booking b, string id) => b.Item.OwnerId == id;

    private static readonly Rule ApproveRule = new(
        new[] { BookingStatus.Pending }, BookingStatus.Approved,
        IsOwner, (b, _) => b.Renter,
        b => $"تمت الموافقة على طلب حجزك لـ \"{b.Item.Title}\".",
        "تمت الموافقة على حجزك", RecheckAvailability: true);

    private static readonly Rule RejectRule = new(
        new[] { BookingStatus.Pending }, BookingStatus.Rejected,
        IsOwner, (b, _) => b.Renter,
        b => $"تم رفض طلب حجزك لـ \"{b.Item.Title}\".",
        "تم رفض طلب الحجز");

    private static readonly Rule CancelRule = new(
        new[] { BookingStatus.Pending, BookingStatus.Approved }, BookingStatus.Cancelled,
        (b, id) => b.RenterId == id || b.Item.OwnerId == id,
        (b, actor) => b.RenterId == actor ? b.Item.Owner : b.Renter,     // notify the other party
        b => $"تم إلغاء الحجز الخاص بـ \"{b.Item.Title}\".",
        "تم إلغاء الحجز");

    private static readonly Rule ActivateRule = new(
        new[] { BookingStatus.Approved }, BookingStatus.Active,
        IsOwner, (b, _) => b.Renter,
        b => $"تم تسليم \"{b.Item.Title}\" وبدأت فترة الإيجار.",
        "بدأت فترة الإيجار");

    private static readonly Rule CompleteRule = new(
        new[] { BookingStatus.Active }, BookingStatus.Completed,
        IsOwner, (b, _) => b.Renter,
        b => $"اكتمل حجز \"{b.Item.Title}\". يمكنك الآن تقييم التجربة.",
        "اكتمل الحجز");

    private async Task<ServiceResult<Booking>> TransitionAsync(
        int bookingId, string actorId, Rule rule, CancellationToken ct)
    {
        if (bookingId <= 0 || string.IsNullOrWhiteSpace(actorId))
            return ServiceResult<Booking>.Fail(NotFoundOrDenied);

        try
        {
            var outcome = await RunAtomicAsync(async () =>
            {
                var booking = await _db.Bookings
                    .Include(b => b.Item).ThenInclude(i => i.Owner)
                    .Include(b => b.Renter)
                    .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

                // "Not found" and "not yours" return the same message, so ids cannot be enumerated (IDOR).
                if (booking is null || !rule.IsAllowedActor(booking, actorId))
                {
                    if (booking is not null)
                        _logger.LogWarning("User {UserId} attempted {To} on booking {BookingId} without permission",
                            actorId, rule.To, bookingId);
                    return Outcome.Fail(NotFoundOrDenied);
                }

                if (!rule.From.Contains(booking.Status))
                    return Outcome.Fail($"لا يمكن تنفيذ هذا الإجراء والحجز في حالة {booking.Status}.");

                if (rule.RecheckAvailability)
                {
                    if (!booking.Item.IsActive)
                        return Outcome.Fail("هذا العنصر لم يعد متاحاً للإيجار.");
                    if (booking.StartDate.Date < DateTime.UtcNow.Date)
                        return Outcome.Fail("انتهى موعد بدء هذا الحجز.");
                    if (!await IsAvailableAsync(booking.ItemId, booking.StartDate, booking.EndDate, booking.Id, ct))
                        return Outcome.Fail("لا يمكن الموافقة: الفترة أصبحت محجوزة أو محظورة.");
                }

                booking.Status = rule.To;

                var target = rule.Recipient(booking, actorId);
                var text = TextUtil.Clean(rule.Message(booking), 255);
                _db.Notifications.Add(new Notification { UserId = target.Id, Message = text });

                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Booking {BookingId} -> {Status} by {UserId}", bookingId, rule.To, actorId);
                return new Outcome(ServiceResult<Booking>.Ok(booking), target.Email, rule.Subject, text);
            }, ct);

            await NotifyByEmailAsync(outcome, ct);
            return outcome.Result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Another request changed the same booking at the same time (needs a RowVersion column to be raised).
            _logger.LogWarning(ex, "Concurrency conflict on booking {BookingId}", bookingId);
            return ServiceResult<Booking>.Fail(Conflict);
        }
        catch (Exception ex) when (ex is DbUpdateException or System.Data.Common.DbException)
        {
            _logger.LogError(ex, "Transition to {Status} failed for booking {BookingId}", rule.To, bookingId);
            return ServiceResult<Booking>.Fail(TryAgain);
        }
    }

    // ───────────────────────── Internals ─────────────────────────

    private sealed record Outcome(ServiceResult<Booking> Result, string? Email = null, string Subject = "", string Text = "")
    {
        public static Outcome Fail(string error) => new(ServiceResult<Booking>.Fail(error));
    }

    /// <summary>
    /// Runs work in a serializable transaction (range locks stop two requests from booking the same dates).
    /// Uses the EF execution strategy so it stays compatible with EnableRetryOnFailure.
    /// The non-relational provider (InMemory, used in unit tests) just runs the work.
    /// </summary>
    private async Task<T> RunAtomicAsync<T>(Func<Task<T>> work, CancellationToken ct)
    {
        if (!_db.Database.IsRelational())
            return await work();

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _db.ChangeTracker.Clear();   // a retry must not see entities from a failed attempt
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var result = await work();
            await tx.CommitAsync(ct);
            return result;
        });
    }

    private async Task NotifyByEmailAsync(Outcome outcome, CancellationToken ct)
    {
        // Mail goes out after the commit. A mail failure must never roll back or fail the business operation.
        if (!outcome.Result.Succeeded || string.IsNullOrWhiteSpace(outcome.Email)) return;
        try
        {
            await _email.SendAsync(outcome.Email, outcome.Subject, outcome.Text, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send booking e-mail");
        }
    }

    private static BookingPriceBreakdown Calculate(Item item, DateTime start, DateTime end)
    {
        var days = Math.Max(1, (end.Date - start.Date).Days);
        var rental = Math.Round(days * item.PricePerDay, 2);
        var fee = Math.Round(rental * PlatformFeeRate, 2);
        return new BookingPriceBreakdown(days, rental, item.SecurityDeposit, fee);
    }

    private static string Fmt(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}