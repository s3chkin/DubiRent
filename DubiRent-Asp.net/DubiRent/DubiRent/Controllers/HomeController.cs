using System.Diagnostics;
using System.Threading.Tasks;
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

        public async Task<IActionResult> Index(PropertySearchModel search)
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

            var properties = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .ToListAsync();

            // Check which properties are in user's favourites
            var userId = _userManager.GetUserId(User);
            HashSet<int> favouritePropertyIds = new HashSet<int>();
            if (!string.IsNullOrEmpty(userId) && properties.Any())
            {
                var propertyIds = properties.Select(p => p.Id).ToList();
                var favourites = await db.Favourites
                    .Where(f => f.UserId == userId && propertyIds.Contains(f.PropertyId))
                    .Select(f => f.PropertyId)
                    .ToListAsync();
                favouritePropertyIds = favourites.ToHashSet();
            }

            ViewBag.FavouritePropertyIds = favouritePropertyIds;

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

        // GET: Contact
        public IActionResult Contact()
        {
            var userId = _userManager.GetUserId(User);
            var model = new ContactModel();
            
            // Pre-fill email if user is logged in
            if (!string.IsNullOrEmpty(userId))
            {
                var user = _userManager.GetUserAsync(User).Result;
                if (user != null)
                {
                    model.Email = user.Email ?? "";
                    model.Name = user.UserName ?? "";
                }
            }
            
            return View(model);
        }

        // POST: Contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            
            var message = new Message
            {
                UserId = userId,
                Name = model.Name,
                Email = model.Email,
                MessageText = model.Message,
                PropertyId = model.PropertyId,
                CreatedAt = DateTime.Now
            };

            db.Messages.Add(message);
            await db.SaveChangesAsync();

            TempData["Success"] = "Thank you for contacting us! We will get back to you soon.";
            return RedirectToAction(nameof(Contact));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
