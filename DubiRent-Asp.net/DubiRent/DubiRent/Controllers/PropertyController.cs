using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DubiRent.Data;
using DubiRent.Data.Models;
using Microsoft.AspNetCore.Identity;
using DubiRent.Models;
using DubiRent.Services;

namespace DubiRent.Controllers
{
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private string[] allowedExtention = new[] { "png", "jpg", "jpeg" };
        public PropertyController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment, UserManager<AppUser> userManager, IEmailService emailService)
        {
            this.db = db;
            this.webHostEnvironment = webHostEnvironment;
            _emailService = emailService;
            this._userManager = userManager;
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

        // Admin Panel
        public IActionResult Admin(string statusFilter = null, int page = 1, int pageSize = 12)
        {
            // Seed initial locations if none exist
            if (!db.Locations.Any())
            {
                SeedLocations();
            }

            // Get all properties query with includes
            IQueryable<Property> allPropertiesQuery = db.Properties
                .Include(p => p.Location)
                .Include(p => p.Images);

            // Apply status filter if provided
            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<PropertyStatus>(statusFilter, out var status))
            {
                allPropertiesQuery = allPropertiesQuery.Where(p => p.Status == status);
            }

            // Get total count before pagination
            var totalCount = allPropertiesQuery.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply sorting and pagination
            var properties = allPropertiesQuery
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Get all properties for statistics (without filter)
            var allPropertiesForStats = db.Properties
                .Include(p => p.Location)
                .Include(p => p.Images)
                .ToList();

            // Pass all properties to ViewBag for statistics
            ViewBag.AllProperties = allPropertiesForStats;
            ViewBag.CurrentFilter = statusFilter;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            return View(properties);
        }

        // GET: Create Property
        public IActionResult Create()
        {
            // Seed initial locations if none exist
            if (!db.Locations.Any())
            {
                SeedLocations();
            }

            ViewBag.Locations = new SelectList(db.Locations.ToList(), "Id", "Name");
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(PropertyStatus)), PropertyStatus.Available);
            
            var model = new InputPropertyModel();
            return View(model);
        }

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

        // POST: Create Property
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InputPropertyModel model)
        {
            // Get uploaded images from Request.Form.Files
            var uploadedImages = Request.Form.Files.Where(f => f.Name == "Images" && f.Length > 0).ToList();

            // Validate images if provided
            if (uploadedImages.Any())
            {
                foreach (var image in uploadedImages)
                {
                    if (image != null && image.Length > 0)
                    {
                        var extension = Path.GetExtension(image.FileName).ToLowerInvariant().Replace(".", "");
                        if (!allowedExtention.Contains(extension))
                        {
                            ModelState.AddModelError("Images", $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", allowedExtention)}");
                        }
                        if (image.Length > 5 * 1024 * 1024) // 5MB limit
                        {
                            ModelState.AddModelError("Images", $"File '{image.FileName}' exceeds the maximum file size of 5MB.");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                var property = new Property
                {
                    Title = model.Title,
                    Address = model.Address,
                    Bathrooms = model.Bathrooms,
                    Bedrooms = model.Bedrooms,
                    CreatedAt = DateTime.Now,
                    Description = model.Description,
                    IsActive = model.IsActive,
                    PricePerMonth = model.PricePerMonth,
                    SquareMeters = model.SquareMeters,
                    Status = model.Status,
                    LocationId = model.LocationId,
                };

                db.Properties.Add(property);
                await db.SaveChangesAsync();

                // Handle image uploads
                if (uploadedImages.Any())
                {
                    var imagesFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", "properties");
                    var uploadedFilePaths = new List<string>(); // Track uploaded files for rollback
                    
                    try
                    {
                        // Create directory if it doesn't exist
                        if (!Directory.Exists(imagesFolder))
                        {
                            Directory.CreateDirectory(imagesFolder);
                        }

                        // Get main image index from form
                        int mainImageIndex = 0;
                        if (Request.Form.TryGetValue("MainImageIndex", out var mainImageIndexValue) && 
                            int.TryParse(mainImageIndexValue, out var parsedIndex) && 
                            parsedIndex >= 0 && parsedIndex < uploadedImages.Count)
                        {
                            mainImageIndex = parsedIndex;
                        }

                        int imageIndex = 0;
                        foreach (var imageFile in uploadedImages)
                        {
                            if (imageFile != null && imageFile.Length > 0)
                            {
                                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                                var uniqueFileName = $"{property.Id}_{imageIndex}_{Guid.NewGuid()}{extension}";
                                var filePath = Path.Combine(imagesFolder, uniqueFileName);

                                try
                                {
                                    using (var stream = new FileStream(filePath, FileMode.Create))
                                    {
                                        await imageFile.CopyToAsync(stream);
                                    }
                                    
                                    uploadedFilePaths.Add(filePath); // Track successful uploads

                                    var propertyImage = new PropertyImage
                                    {
                                        PropertyId = property.Id,
                                        ImageUrl = $"/images/properties/{uniqueFileName}",
                                        IsMain = imageIndex == mainImageIndex,
                                        CreatedAt = DateTime.Now
                                    };

                                    db.PropertyImages.Add(propertyImage);
                                }
                                catch (Exception ex)
                                {
                                    // Delete any uploaded files if error occurs
                                    foreach (var uploadedPath in uploadedFilePaths)
                                    {
                                        try
                                        {
                                            if (System.IO.File.Exists(uploadedPath))
                                            {
                                                System.IO.File.Delete(uploadedPath);
                                            }
                                        }
                                        catch { }
                                    }
                                    
                                    TempData["Error"] = $"Error uploading image '{imageFile.FileName}': {ex.Message}";
                                    ViewBag.Locations = new SelectList(db.Locations.ToList(), "Id", "Name", model.LocationId);
                                    ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(PropertyStatus)), model.Status);
                                    return View(model);
                                }
                            }
                            imageIndex++;
                        }

                        await db.SaveChangesAsync();
                        
                        // Verify that images were saved correctly
                        await db.Entry(property).Collection(p => p.Images).LoadAsync();
                        if (!property.Images.Any())
                        {
                            // Images were not saved - this should not happen, but handle it gracefully
                            TempData["Warning"] = "Property was created, but images may not have been saved. Please edit the property to add images.";
                        }
                    }
                    catch (Exception ex)
                    {
                        // Rollback: Delete any uploaded files if database save fails
                        foreach (var uploadedPath in uploadedFilePaths)
                        {
                            try
                            {
                                if (System.IO.File.Exists(uploadedPath))
                                {
                                    System.IO.File.Delete(uploadedPath);
                                }
                            }
                            catch { }
                        }
                        
                        TempData["Error"] = $"Error saving property images: {ex.Message}";
                        ViewBag.Locations = new SelectList(db.Locations.ToList(), "Id", "Name", model.LocationId);
                        ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(PropertyStatus)), model.Status);
                        return View(model);
                    }
                }

                TempData["Success"] = "Property added successfully!";
                return RedirectToAction(nameof(Admin));
            }

            // Reload ViewBag data if model is invalid
            ViewBag.Locations = new SelectList(db.Locations.ToList(), "Id", "Name", model.LocationId);
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(PropertyStatus)), model.Status);
            return View(model);
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
            if (!string.IsNullOrEmpty(userId))
            {
                existingRequest = await db.ViewingRequests
                    .FirstOrDefaultAsync(vr => vr.PropertyId == id && vr.UserId == userId);
            }

            ViewBag.HasExistingRequest = existingRequest != null;
            ViewBag.ExistingRequest = existingRequest;
            ViewBag.PropertyTitle = property.Title;

            // Check if property is in user's favorites
            bool isFavourite = false;
            if (!string.IsNullOrEmpty(userId))
            {
                isFavourite = await db.Favourites
                    .AnyAsync(f => f.PropertyId == id && f.UserId == userId);
            }
            ViewBag.IsFavourite = isFavourite;

            return View(property);
        }

        // GET: Edit Property
        public IActionResult Edit(int id)
        {
            // Seed initial locations if none exist
            if (!db.Locations.Any())
            {
                SeedLocations();
            }

            var property = db.Properties
                .Include(p => p.Location)
                .Include(p => p.Images)
                .FirstOrDefault(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            var model = new InputPropertyModel
            {
                Id = property.Id,
                Title = property.Title,
                Description = property.Description,
                PricePerMonth = property.PricePerMonth,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                SquareMeters = property.SquareMeters,
                LocationId = property.LocationId,
                Address = property.Address,
                Status = property.Status,
                IsActive = property.IsActive
            };

            ViewBag.Locations = new SelectList(db.Locations.ToList(), "Id", "Name", property.LocationId);
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(PropertyStatus)), property.Status);
            ViewBag.ExistingImages = property.Images?.ToList() ?? new List<PropertyImage>();

            return View(model);
        }

        // POST: Edit Property
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InputPropertyModel model)
        {
            if (model.Id == null)
            {
                return NotFound();
            }

            var property = db.Properties
                .Include(p => p.Images)
                .FirstOrDefault(p => p.Id == model.Id);

            if (property == null)
            {
                return NotFound();
            }

            // Get uploaded images from Request.Form.Files
            var uploadedImages = Request.Form.Files.Where(f => f.Name == "Images" && f.Length > 0).ToList();

            // Get images to delete
            var imagesToDelete = Request.Form["ImagesToDelete"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.Parse(id))
                .ToList();

            // Get selected main image ID
            int? mainImageId = null;
            if (!string.IsNullOrEmpty(Request.Form["MainImageId"].ToString()))
            {
                mainImageId = int.Parse(Request.Form["MainImageId"].ToString());
            }

            // Remove any validation errors for Images field - it's optional in Edit
            ModelState.Remove("Images");

            // Validate images if provided (only validate format and size, not required)
            if (uploadedImages.Any())
            {
                foreach (var image in uploadedImages)
                {
                    if (image != null && image.Length > 0)
                    {
                        var extension = Path.GetExtension(image.FileName).ToLowerInvariant().Replace(".", "");
                        if (!allowedExtention.Contains(extension))
                        {
                            ModelState.AddModelError("Images", $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", allowedExtention)}");
                        }
                        if (image.Length > 5 * 1024 * 1024) // 5MB limit
                        {
                            ModelState.AddModelError("Images", $"File '{image.FileName}' exceeds the maximum file size of 5MB.");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                // Update property fields
                property.Title = model.Title;
                property.Description = model.Description;
                property.PricePerMonth = model.PricePerMonth;
                property.Bedrooms = model.Bedrooms;
                property.Bathrooms = model.Bathrooms;
                property.SquareMeters = model.SquareMeters;
                property.LocationId = model.LocationId;
                property.Address = model.Address;
                property.Status = model.Status;
                property.IsActive = model.IsActive;
                property.UpdatedAt = DateTime.Now;

                // Delete selected images first (before updating main image)
                if (imagesToDelete.Any())
                {
                    var imagesToRemove = property.Images?.Where(img => imagesToDelete.Contains(img.Id)).ToList();
                    if (imagesToRemove != null)
                    {
                        foreach (var img in imagesToRemove)
                        {
                            // Delete physical file
                            var imagePath = Path.Combine(webHostEnvironment.WebRootPath, img.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }

                            db.PropertyImages.Remove(img);
                        }
                    }
                }

                // Handle main image selection
                if (property.Images != null)
                {
                    if (mainImageId.HasValue)
                    {
                        // User selected a main image - reset all and set the selected one
                        foreach (var img in property.Images)
                        {
                            img.IsMain = false;
                        }
                        
                        var mainImage = property.Images.FirstOrDefault(img => img.Id == mainImageId.Value && !imagesToDelete.Contains(img.Id));
                        if (mainImage != null)
                        {
                            mainImage.IsMain = true;
                        }
                    }
                    else
                    {
                        // No main image selected - check if current main image will be deleted
                        var currentMainImage = property.Images.FirstOrDefault(img => img.IsMain);
                        if (currentMainImage != null && imagesToDelete.Contains(currentMainImage.Id))
                        {
                            // Current main image is being deleted - reset all
                            foreach (var img in property.Images)
                            {
                                img.IsMain = false;
                            }
                            // Will set first remaining image as main after deletion
                        }
                        // Otherwise, keep current main image
                    }
                }
                
                // After deletion logic, we need to check remaining images after they are removed from the collection
                // This will be handled after SaveChanges when we reload the images

                // Handle new image uploads
                if (uploadedImages.Any())
                {
                    var imagesFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", "properties");
                    
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(imagesFolder))
                    {
                        Directory.CreateDirectory(imagesFolder);
                    }

                    int imageIndex = property.Images?.Count ?? 0;
                    foreach (var imageFile in uploadedImages)
                    {
                        if (imageFile != null && imageFile.Length > 0)
                        {
                            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                            var uniqueFileName = $"{property.Id}_{imageIndex}_{Guid.NewGuid()}{extension}";
                            var filePath = Path.Combine(imagesFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(stream);
                            }

                            var propertyImage = new PropertyImage
                            {
                                PropertyId = property.Id,
                                ImageUrl = $"/images/properties/{uniqueFileName}",
                                IsMain = false, // New images are never main - user must select from existing or newly uploaded
                                CreatedAt = DateTime.Now
                            };

                            db.PropertyImages.Add(propertyImage);
                            imageIndex++;
                        }
                    }
                }

                await db.SaveChangesAsync();

                // Ensure there's a main image after all operations
                if (property.Images != null && property.Images.Any())
                {
                    var hasMainImage = property.Images.Any(img => img.IsMain);
                    if (!hasMainImage)
                    {
                        var firstImage = property.Images.FirstOrDefault();
                        if (firstImage != null)
                        {
                            firstImage.IsMain = true;
                            await db.SaveChangesAsync();
                        }
                    }
                }

                TempData["Success"] = "Property updated successfully!";
                return RedirectToAction(nameof(Admin));
            }

            // Reload ViewBag data if model is invalid
            ViewBag.Locations = new SelectList(db.Locations.ToList(), "Id", "Name", model.LocationId);
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(PropertyStatus)), model.Status);
            ViewBag.ExistingImages = property.Images?.ToList() ?? new List<PropertyImage>();

            return View(model);
        }

        // DELETE: Delete Property
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Validate: Check if property exists
            var property = await db.Properties
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                TempData["Error"] = "Property not found.";
                return RedirectToAction(nameof(Admin));
            }

            try
            {
                // Delete all physical image files
                if (property.Images != null && property.Images.Any())
                {
                    foreach (var image in property.Images)
                    {
                        var imagePath = Path.Combine(webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            try
                            {
                                System.IO.File.Delete(imagePath);
                            }
                            catch
                            {
                                // Log error but continue deletion
                                // File might be locked or already deleted
                            }
                        }
                    }
                }

                // Remove property from database
                // Cascade delete will handle related records (Images, ViewingRequests, Payments, etc.)
                db.Properties.Remove(property);
                await db.SaveChangesAsync();

                TempData["Success"] = $"Property '{property.Title}' has been deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while deleting the property: {ex.Message}";
            }

            return RedirectToAction(nameof(Admin));
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

        // GET: Viewing Requests (Admin)
        public IActionResult ViewingRequests()
        {
            var requests = db.ViewingRequests
                .Include(vr => vr.Property)
                    .ThenInclude(p => p.Location)
                .Include(vr => vr.Property)
                    .ThenInclude(p => p.Images)
                .OrderByDescending(vr => vr.CreatedAt)
                .ToList();

            return View(requests);
        }

        // POST: Update Viewing Request Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRequestStatus(int id, int status)
        {
            var request = await db.ViewingRequests
                .Include(vr => vr.Property)
                .FirstOrDefaultAsync(vr => vr.Id == id);
            
            if (request == null)
            {
                TempData["Error"] = "Viewing request not found.";
                return RedirectToAction(nameof(ViewingRequests));
            }

            if (!Enum.IsDefined(typeof(ViewingRequestStatus), status))
            {
                TempData["Error"] = "Invalid status value.";
                return RedirectToAction(nameof(ViewingRequests));
            }

            var oldStatus = request.Status;
            request.Status = (ViewingRequestStatus)status;
            request.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();

            // Send email notification if status changed to Approved
            if (status == (int)ViewingRequestStatus.Approved && oldStatus != ViewingRequestStatus.Approved)
            {
                try
                {
                    await _emailService.SendViewingRequestApprovedEmailAsync(
                        request.Email,
                        request.FullName,
                        request.Property?.Title ?? "Property",
                        request.PreferredDate,
                        request.PreferredTime
                    );
                }
                catch (Exception ex)
                {
                    // Log error but don't break the flow
                    // Email failure shouldn't prevent status update
                    TempData["Warning"] = "Viewing request approved, but email notification could not be sent.";
                }
            }

            TempData["Success"] = "Viewing request status updated successfully!";
            return RedirectToAction(nameof(ViewingRequests));
        }

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
