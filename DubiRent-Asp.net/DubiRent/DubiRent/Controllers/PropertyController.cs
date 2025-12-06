using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DubiRent.Data;
using DubiRent.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using DubiRent.Models;
using DubiRent.Services;

namespace DubiRent.Controllers
{
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private string[] allowedExtention = new[] { "png", "jpg", "jpeg","webp" };
        public PropertyController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IEmailService emailService)
        {
            this.db = db;
            this.webHostEnvironment = webHostEnvironment;
            _emailService = emailService;
            this._userManager = userManager;
            this._roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Properties(PropertySearchModel search)
        {
            // Seed initial locations if none exist
            if (!db.Locations.Any())
            {
                SeedLocations();
            }

            var query = db.Properties
                .Include(p => p.Location)
                .Include(p => p.Images)
                .Where(p => p.IsActive && p.Status == PropertyStatus.Available)
                .AsQueryable();

            // Apply search filters
            if (!string.IsNullOrWhiteSpace(search.Title))
            {
                query = query.Where(p => EF.Functions.Like(p.Title, $"%{search.Title}%"));
            }

            if (!string.IsNullOrWhiteSpace(search.Address))
            {
                query = query.Where(p => EF.Functions.Like(p.Address, $"%{search.Address}%"));
            }

            if (!string.IsNullOrWhiteSpace(search.LocationName))
            {
                query = query.Where(p => EF.Functions.Like(p.Location.Name, $"%{search.LocationName}%") || EF.Functions.Like(p.Location.City, $"%{search.LocationName}%"));
            }
            else if (search.LocationId.HasValue && search.LocationId.Value > 0)
            {
                query = query.Where(p => p.LocationId == search.LocationId.Value);
            }

            if (search.MinPrice.HasValue)
            {
                query = query.Where(p => p.PricePerMonth >= search.MinPrice.Value);
            }

            if (search.MaxPrice.HasValue)
            {
                query = query.Where(p => p.PricePerMonth <= search.MaxPrice.Value);
            }

            if (search.MinSquareMeters.HasValue)
            {
                query = query.Where(p => p.SquareMeters >= search.MinSquareMeters.Value);
            }

            if (search.MaxSquareMeters.HasValue)
            {
                query = query.Where(p => p.SquareMeters <= search.MaxSquareMeters.Value);
            }

            if (search.Bedrooms.HasValue)
            {
                if (search.Bedrooms.Value >= 5)
                {
                    // For 5+, search for properties with 5 or more bedrooms
                    query = query.Where(p => p.Bedrooms >= 5);
                }
                else
                {
                    query = query.Where(p => p.Bedrooms == search.Bedrooms.Value);
                }
            }

            if (search.Bathrooms.HasValue)
            {
                if (search.Bathrooms.Value >= 5)
                {
                    // For 5+, search for properties with 5 or more bathrooms
                    query = query.Where(p => p.Bathrooms >= 5);
                }
                else
                {
                    query = query.Where(p => p.Bathrooms == search.Bathrooms.Value);
                }
            }

            // Apply sorting
            switch (search.SortBy)
            {
                case "price_asc":
                    query = query.OrderBy(p => p.PricePerMonth);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.PricePerMonth);
                    break;
                case "size_desc":
                    query = query.OrderByDescending(p => p.SquareMeters);
                    break;
                case "newest":
                default:
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            // Get total count before pagination
            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)search.PageSize);
            
            // Apply pagination
            var properties = query
                .Skip((search.Page - 1) * search.PageSize)
                .Take(search.PageSize)
                .ToList();

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
            ViewBag.Locations = new SelectList(db.Locations.OrderBy(l => l.Name).ToList(), "Id", "Name", search.LocationId);
            ViewBag.SearchModel = search;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = search.Page;
            ViewBag.PageSize = search.PageSize;

            return View(properties);
        }

        // Admin Panel - MOVED TO AdminController.Index
        // Create Property - MOVED TO AdminController.Create
        // Edit Property - MOVED TO AdminController.Edit
        // Delete Property - MOVED TO AdminController.Delete
        // Viewing Requests - MOVED TO AdminController.ViewingRequests
        // Update Request Status - MOVED TO AdminController.UpdateRequestStatus

        private void SeedLocations()
        {
            var locations = new List<Location>
            {
                new Location { Name = "Dubai Marina", City = "Dubai" },
                new Location { Name = "Downtown Dubai", City = "Dubai" },
                new Location { Name = "Palm Jumeirah", City = "Dubai" },
                new Location { Name = "Business Bay", City = "Dubai" },
                new Location { Name = "JBR (Jumeirah Beach Residence)", City = "Dubai" },
                new Location { Name = "Dubai Hills", City = "Dubai" },
                new Location { Name = "Dubai Creek Harbour", City = "Dubai" }
            };

            db.Locations.AddRange(locations);
            db.SaveChanges();
        }

        // GET: Property Details
        public async Task<IActionResult> Details(int id)
        {
            var property = await db.Properties
                .Include(p => p.Location)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            // Check if user already has a viewing request for this property
            var userId = _userManager.GetUserId(User);
            ViewingRequest? existingRequest = null;
            bool isAdmin = false;
            
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                }
                
                // Only check for existing request if user is not admin
                if (!isAdmin)
                {
                    existingRequest = await db.ViewingRequests
                        .FirstOrDefaultAsync(vr => vr.PropertyId == id && vr.UserId == userId);
                }
            }

            ViewBag.HasExistingRequest = existingRequest != null;
            ViewBag.ExistingRequest = existingRequest;
            ViewBag.PropertyTitle = property.Title;
            ViewBag.IsAdmin = isAdmin;

            // If user is admin, load all viewing requests for this property
            if (isAdmin)
            {
                var allRequests = await db.ViewingRequests
                    .Include(vr => vr.Property)
                    .Where(vr => vr.PropertyId == id)
                    .OrderByDescending(vr => vr.CreatedAt)
                    .ToListAsync();
                ViewBag.ViewingRequestsForProperty = allRequests;
            }

            // Check if property is in user's favorites
            bool isFavourite = false;
            if (!string.IsNullOrEmpty(userId))
            {
                isFavourite = await db.Favourites
                    .AnyAsync(f => f.PropertyId == id && f.UserId == userId);
            }
            ViewBag.IsFavourite = isFavourite;

            // Check if user has already paid for this property
            bool hasPaid = false;
            if (!string.IsNullOrEmpty(userId))
            {
                hasPaid = await db.Payments
                    .AnyAsync(p => p.PropertyId == id && p.UserId == userId && p.PaymentStatus == PaymentStatus.Completed);
            }
            ViewBag.HasPaid = hasPaid;

            // Check if user has an approved viewing request for this property (required for payment)
            bool hasApprovedRequest = false;
            bool hasPendingRequest = false;
            if (!string.IsNullOrEmpty(userId) && !isAdmin)
            {
                hasApprovedRequest = await db.ViewingRequests
                    .AnyAsync(vr => vr.PropertyId == id 
                        && vr.UserId == userId 
                        && vr.Status == ViewingRequestStatus.Approved);
                
                // Check if user has submitted a request (Pending or Cancelled)
                hasPendingRequest = await db.ViewingRequests
                    .AnyAsync(vr => vr.PropertyId == id 
                        && vr.UserId == userId);
            }
            ViewBag.HasApprovedRequest = hasApprovedRequest;
            ViewBag.HasPendingRequest = hasPendingRequest;

            return View(property);
        }

        // POST: Request Viewing
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestViewing(ViewingRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in all required fields.";
                return RedirectToAction(nameof(Details), new { id = model.PropertyId });
            }

            // Check if property exists
            var property = await db.Properties.FindAsync(model.PropertyId);
            if (property == null)
            {
                TempData["Error"] = "Property not found.";
                return RedirectToAction(nameof(Properties));
            }

            // REQUIRE LOGIN: Check if user is logged in
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Please log in to request a viewing.";
                return RedirectToAction(nameof(Details), new { id = model.PropertyId });
            }

            // Check if date is in the future
            if (model.PreferredDate.Date <= DateTime.Now.Date)
            {
                TempData["Error"] = "Preferred date must be in the future.";
                return RedirectToAction(nameof(Details), new { id = model.PropertyId });
            }

            // Check if user already has a viewing request for this property
            var existingRequest = await db.ViewingRequests
                .FirstOrDefaultAsync(vr => vr.PropertyId == model.PropertyId && vr.UserId == userId);
            
            if (existingRequest != null)
            {
                TempData["Error"] = "You have already submitted a viewing request for this property. Please wait for a response.";
                return RedirectToAction(nameof(Details), new { id = model.PropertyId });
            }

            // Get user email from logged in user
            var user = await _userManager.GetUserAsync(User);
            var userEmail = user?.Email ?? "";

            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "Unable to retrieve your email. Please update your profile.";
                return RedirectToAction(nameof(Details), new { id = model.PropertyId });
            }

            var viewingRequest = new ViewingRequest
            {
                PropertyId = model.PropertyId,
                UserId = userId,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Email = userEmail,
                PreferredDate = model.PreferredDate,
                PreferredTime = model.PreferredTime,
                Status = ViewingRequestStatus.Pending,
                CreatedAt = DateTime.Now
            };

            db.ViewingRequests.Add(viewingRequest);
            await db.SaveChangesAsync();

            TempData["Success"] = "Viewing request submitted successfully! We will contact you soon.";
            return RedirectToAction(nameof(Details), new { id = model.PropertyId });
        }

        // Viewing Requests (Admin) - MOVED TO AdminController.ViewingRequests
        // Update Request Status (Admin) - MOVED TO AdminController.UpdateRequestStatus

        // POST: Toggle Favourite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavourite(int propertyId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please log in to add favorites." });
            }

            var property = await db.Properties.FindAsync(propertyId);
            if (property == null)
            {
                return Json(new { success = false, message = "Property not found." });
            }

            var existingFavourite = await db.Favourites
                .FirstOrDefaultAsync(f => f.PropertyId == propertyId && f.UserId == userId);

            if (existingFavourite != null)
            {
                // Remove from favourites
                db.Favourites.Remove(existingFavourite);
                await db.SaveChangesAsync();
                return Json(new { success = true, isFavourite = false, message = "Removed from favourites" });
            }
            else
            {
                // Add to favourites
                var favourite = new Favourite
                {
                    UserId = userId,
                    PropertyId = propertyId,
                    CreatedAt = DateTime.Now
                };
                db.Favourites.Add(favourite);
                await db.SaveChangesAsync();
                return Json(new { success = true, isFavourite = true, message = "Added to favourites" });
            }
        }

        // GET: My Favourites
        public async Task<IActionResult> MyFavourites()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Please log in to view your favourites.";
                return RedirectToAction(nameof(Properties));
            }

            var favouriteProperties = await db.Favourites
                .Include(f => f.Property)
                    .ThenInclude(p => p.Location)
                .Include(f => f.Property)
                    .ThenInclude(p => p.Images)
                .Where(f => f.UserId == userId)
                .Select(f => f.Property)
                .Where(p => p != null && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.TotalCount = favouriteProperties.Count;

            return View(favouriteProperties);
        }
    }
}
