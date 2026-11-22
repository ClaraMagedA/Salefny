using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;

namespace SalefnyMVC.Services
{
    public interface IRatingService
    {
        Task<decimal> RecalculateAsync(string userId);
    }

    public class RatingService : IRatingService
    {
        private readonly SalefnyDbContext _context;

        public RatingService(SalefnyDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> RecalculateAsync(string userId)
        {
            var ratings = await _context.Reviews
                .Where(r => r.RevieweeId == userId)
                .Select(r => r.Rating)
                .ToListAsync();

            var average = BusinessRules.AverageRating(ratings);
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.TrustRating = average;
                user.AverageRating = average;
                await _context.SaveChangesAsync();
            }
            return average;
        }
    }
}
