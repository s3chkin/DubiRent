using System.Diagnostics;
using System.Threading.Tasks;
using DubiRent.Data.Models;
using DubiRent.Data;
using DubiRent.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OutputCaching;

namespace DubiRent.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<AppUser> _userManager;
        private string[] allowedExtention = new[] { "png", "jpg", "jpeg","webp" };

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db, IWebHostEnvironment webHostEnvironment, UserManager<AppUser> userManager)
        {
            _logger = logger;
            this.db = db;
            this.webHostEnvironment = webHostEnvironment;
            this._userManager = userManager;
        }

        // Cache Home page for 10 minutes (vary by search parameters and authentication status)
        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "Title", "Address", "LocationId", "LocationName", "MinPrice", "MaxPrice", "MinSquareMeters", "MaxSquareMeters" }, VaryByHeader = "Cookie")]
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

            // Get popular locations (locations with most active properties, max 4)
            var locationStats = await db.Properties
                .Where(p => p.IsActive && p.Status == PropertyStatus.Available)
                .Include(p => p.Location)
                .GroupBy(p => new { p.LocationId, p.Location.Name, p.Location.City, p.Location.ImageUrl })
                .Select(g => new
                {
                    LocationId = g.Key.LocationId,
                    LocationName = g.Key.Name,
                    City = g.Key.City,
                    ImageUrl = g.Key.ImageUrl,
                    PropertyCount = g.Count()
                })
                .OrderByDescending(x => x.PropertyCount)
                .Take(4)
                .ToListAsync();

            var popularLocations = locationStats.Select(x => new
            {
                LocationId = x.LocationId,
                LocationName = x.LocationName,
                City = x.City,
                PropertyCount = x.PropertyCount,
                ImageUrl = x.ImageUrl ?? "https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=800"
            }).ToList();

            ViewBag.PopularLocations = popularLocations;

            // If search is performed, redirect to Properties page with search parameters
            if (hasSearch)
            {
                return RedirectToAction("Properties", "Property", search);
            }

            return View(properties);
        }

        // Cache Privacy page for 1 hour (static content)
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Language")]
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

        // Cache Sitemap for 1 day
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Sitemap()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            
            var sitemap = new System.Text.StringBuilder();
            sitemap.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sitemap.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            
            // Home page
            sitemap.AppendLine("  <url>");
            sitemap.AppendLine($"    <loc>{baseUrl}</loc>");
            sitemap.AppendLine("    <lastmod>" + DateTime.Now.ToString("yyyy-MM-dd") + "</lastmod>");
            sitemap.AppendLine("    <changefreq>daily</changefreq>");
            sitemap.AppendLine("    <priority>1.0</priority>");
            sitemap.AppendLine("  </url>");
            
            // Properties page
            sitemap.AppendLine("  <url>");
            sitemap.AppendLine($"    <loc>{baseUrl}/Property/Properties</loc>");
            sitemap.AppendLine("    <lastmod>" + DateTime.Now.ToString("yyyy-MM-dd") + "</lastmod>");
            sitemap.AppendLine("    <changefreq>daily</changefreq>");
            sitemap.AppendLine("    <priority>0.9</priority>");
            sitemap.AppendLine("  </url>");
            
            // Contact page
            sitemap.AppendLine("  <url>");
            sitemap.AppendLine($"    <loc>{baseUrl}/Home/Contact</loc>");
            sitemap.AppendLine("    <lastmod>" + DateTime.Now.ToString("yyyy-MM-dd") + "</lastmod>");
            sitemap.AppendLine("    <changefreq>monthly</changefreq>");
            sitemap.AppendLine("    <priority>0.7</priority>");
            sitemap.AppendLine("  </url>");
            
            // Privacy page
            sitemap.AppendLine("  <url>");
            sitemap.AppendLine($"    <loc>{baseUrl}/Home/Privacy</loc>");
            sitemap.AppendLine("    <lastmod>" + DateTime.Now.ToString("yyyy-MM-dd") + "</lastmod>");
            sitemap.AppendLine("    <changefreq>monthly</changefreq>");
            sitemap.AppendLine("    <priority>0.5</priority>");
            sitemap.AppendLine("  </url>");
            
            // Individual property pages
            var properties = await db.Properties
                .Where(p => p.IsActive && p.Status == PropertyStatus.Available)
                .ToListAsync();
            
            foreach (var property in properties)
            {
                sitemap.AppendLine("  <url>");
                sitemap.AppendLine($"    <loc>{baseUrl}/Property/Details/{property.Id}</loc>");
                sitemap.AppendLine("    <lastmod>" + (property.UpdatedAt ?? property.CreatedAt).ToString("yyyy-MM-dd") + "</lastmod>");
                sitemap.AppendLine("    <changefreq>weekly</changefreq>");
                sitemap.AppendLine("    <priority>0.8</priority>");
                sitemap.AppendLine("  </url>");
            }
            
            sitemap.AppendLine("</urlset>");
            
            return Content(sitemap.ToString(), "application/xml", System.Text.Encoding.UTF8);
        }
    }
}
