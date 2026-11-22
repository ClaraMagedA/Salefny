using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalefnyMVC.Data;
using SalefnyMVC.Models.Enums;

namespace Salefny.Controllers
{
    public class HomeController : Controller
    {
        private readonly SalefnyDbContext _context;

        public HomeController(SalefnyDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ItemImages)
                .Include(i => i.Owner)
                .Where(i => i.IsActive && i.ApprovalStatus == ListingApprovalStatus.Approved)
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Id).ToListAsync();
            return View(items);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult EscrowPolicy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        // Used by UseStatusCodePagesWithReExecute and UseExceptionHandler in Program.cs.
        public IActionResult Error(int? statusCode)
        {
            ViewBag.StatusCode = statusCode;
            return View();
        }
    }
}
