using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Entities;
using SalefnyMVC.Models.Enums;

namespace SalefnyMVC.Services
{
    public interface IEscrowService
    {
        Task HoldAsync(int bookingId, string? actorId = null);
        Task ReleaseAsync(int bookingId, string? actorId = null, string? note = null);
        Task FreezeAsync(int bookingId, string? actorId = null, string? note = null);
        Task DeductAsync(int bookingId, decimal amount, string? actorId = null, string? note = null);
        Task RefundAsync(int bookingId, string? actorId = null, string? note = null);
    }

    public class EscrowService : IEscrowService
    {
        private readonly SalefnyDbContext _context;
        private readonly INotificationService _notifications;

        public EscrowService(SalefnyDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        private async Task<Booking?> GetAsync(int bookingId)
            => await _context.Bookings.Include(b => b.Item).FirstOrDefaultAsync(b => b.Id == bookingId);

        private void Add(int bookingId, EscrowTransactionType type, decimal amount, string? actor, string? note)
        {
            _context.EscrowTransactions.Add(new EscrowTransaction
            {
                BookingId = bookingId,
                Type = type,
                Amount = amount,
                CreatedByAdminId = actor,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task HoldAsync(int bookingId, string? actorId = null)
        {
            var booking = await GetAsync(bookingId);
            if (booking == null || booking.EscrowDepositAmount <= 0) return;
            if (booking.DepositStatus != DepositStatus.NotHeld && booking.DepositStatus != DepositStatus.Released) return;

            booking.DepositStatus = DepositStatus.Held;
            booking.UpdatedAt = DateTime.UtcNow;
            Add(bookingId, EscrowTransactionType.Hold, booking.EscrowDepositAmount, actorId, "حجز مبلغ التأمين بعد موافقة المالك");
            await _context.SaveChangesAsync();
            await _notifications.NotifyAsync(booking.RenterId,
                $"تم حجز مبلغ تأمين بقيمة {booking.EscrowDepositAmount:N2} ج.م للحجز رقم {booking.Id}.",
                "deposit", $"/Bookings/MyBookings");
        }

        public async Task ReleaseAsync(int bookingId, string? actorId = null, string? note = null)
        {
            var booking = await GetAsync(bookingId);
            if (booking == null) return;

            booking.DepositStatus = DepositStatus.Released;
            booking.UpdatedAt = DateTime.UtcNow;
            Add(bookingId, EscrowTransactionType.Release, booking.EscrowDepositAmount, actorId, note ?? "إرجاع مبلغ التأمين بعد اكتمال الحجز");
            await _context.SaveChangesAsync();
            await _notifications.NotifyAsync(booking.RenterId,
                $"تم إرجاع مبلغ التأمين بقيمة {booking.EscrowDepositAmount:N2} ج.م للحجز رقم {booking.Id}.",
                "deposit", $"/Bookings/MyBookings");
        }

        public async Task FreezeAsync(int bookingId, string? actorId = null, string? note = null)
        {
            var booking = await GetAsync(bookingId);
            if (booking == null) return;

            booking.DepositStatus = DepositStatus.Frozen;
            booking.UpdatedAt = DateTime.UtcNow;
            Add(bookingId, EscrowTransactionType.Freeze, booking.EscrowDepositAmount, actorId, note ?? "تجميد التأمين لحين بت النزاع");
            await _context.SaveChangesAsync();
        }

        public async Task DeductAsync(int bookingId, decimal amount, string? actorId = null, string? note = null)
        {
            var booking = await GetAsync(bookingId);
            if (booking == null) return;

            var safeAmount = Math.Min(Math.Max(amount, 0), booking.EscrowDepositAmount);
            var refund = booking.EscrowDepositAmount - safeAmount;

            booking.DepositStatus = DepositStatus.Deducted;
            booking.UpdatedAt = DateTime.UtcNow;
            Add(bookingId, EscrowTransactionType.Deduct, safeAmount, actorId, note ?? "خصم من التأمين بقرار الإدارة");
            if (refund > 0)
                Add(bookingId, EscrowTransactionType.Refund, refund, actorId, "إرجاع المتبقي من التأمين بعد الخصم");

            await _context.SaveChangesAsync();
            await _notifications.NotifyAsync(booking.RenterId,
                $"صدر قرار بشأن الحجز رقم {booking.Id}: خُصم {safeAmount:N2} ج.م وأُرجع {refund:N2} ج.م.",
                "deposit", $"/Bookings/MyBookings");
        }

        public async Task RefundAsync(int bookingId, string? actorId = null, string? note = null)
        {
            var booking = await GetAsync(bookingId);
            if (booking == null) return;

            booking.DepositStatus = DepositStatus.Released;
            booking.UpdatedAt = DateTime.UtcNow;
            Add(bookingId, EscrowTransactionType.Refund, booking.EscrowDepositAmount, actorId, note ?? "إرجاع كامل مبلغ التأمين");
            await _context.SaveChangesAsync();
            await _notifications.NotifyAsync(booking.RenterId,
                $"تم إرجاع مبلغ التأمين كاملاً للحجز رقم {booking.Id}.",
                "deposit", $"/Bookings/MyBookings");
        }
    }
}
