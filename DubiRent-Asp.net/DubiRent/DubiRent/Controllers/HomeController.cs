using System.Diagnostics;
using DubiRent.Data.Models;
using DubiRent.Data;
using DubiRent.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DubiRent.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<AppUser> _userManager;
        private string[] allowedExtention = new[] { "png", "jpg", "jpeg" };

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db, IWebHostEnvironment webHostEnvironment, UserManager<AppUser> userManager)
        {
            _logger = logger;
            this.db = db;
            this.webHostEnvironment = webHostEnvironment;
            this._userManager = userManager;
        }

        public IActionResult Index(PropertySearchModel search)
        {
            var query = db.Properties
                .Include(p => p.Location)
                .Include(p => p.Images)
                .Where(p => p.IsActive && p.Status == PropertyStatus.Available)
                .AsQueryable();

            // Apply search filters only if search is actually performed
            bool hasSearch = false;

            if (!string.IsNullOrWhiteSpace(search.Title))
            {
                query = query.Where(p => EF.Functions.Like(p.Title, $"%{search.Title}%"));
                hasSearch = true;
            }

            if (!string.IsNullOrWhiteSpace(search.Address))
            {
                query = query.Where(p => EF.Functions.Like(p.Address, $"%{search.Address}%"));
                hasSearch = true;
            }

            if (!string.IsNullOrWhiteSpace(search.LocationName))
            {
                query = query.Where(p => EF.Functions.Like(p.Location.Name, $"%{search.LocationName}%") || EF.Functions.Like(p.Location.City, $"%{search.LocationName}%"));
                hasSearch = true;
            }
            else if (search.LocationId.HasValue && search.LocationId.Value > 0)
            {
                query = query.Where(p => p.LocationId == search.LocationId.Value);
                hasSearch = true;
            }

            if (search.MinPrice.HasValue)
            {
                query = query.Where(p => p.PricePerMonth >= search.MinPrice.Value);
                hasSearch = true;
            }

            if (search.MaxPrice.HasValue)
            {
                query = query.Where(p => p.PricePerMonth <= search.MaxPrice.Value);
                hasSearch = true;
            }

            if (search.MinSquareMeters.HasValue)
            {
                query = query.Where(p => p.SquareMeters >= search.MinSquareMeters.Value);
                hasSearch = true;
            }

            if (search.MaxSquareMeters.HasValue)
            {
                query = query.Where(p => p.SquareMeters <= search.MaxSquareMeters.Value);
                hasSearch = true;
            }

            var properties = query
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .ToList();

            // Populate ViewBag for dropdowns
            ViewBag.Locations = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                db.Locations.OrderBy(l => l.Name).ToList(), 
                "Id", 
                "Name", 
                search.LocationId
            );

            // If search is performed, redirect to Properties page with search parameters
            if (hasSearch)
            {
                return RedirectToAction("Properties", "Property", search);
            }

            return View(properties);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
