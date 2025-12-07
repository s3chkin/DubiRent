using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DubiRent.Data;
using DubiRent.Data.Models;
using DubiRent.Models;
using DubiRent.Services;

namespace DubiRent.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IEmailService _emailService;
        private readonly IImageOptimizationService _imageOptimizationService;
        private string[] allowedExtention = new[] { "png", "jpg", "jpeg" };
        private const string DEFAULT_ADMIN_EMAIL = "admin@dubirent.com";
        private const int MAX_IMAGE_WIDTH = 1920; // Maximum width for property images
        private const int MAX_IMAGE_HEIGHT = 1920; // Maximum height for property images
        private const int IMAGE_QUALITY = 85; // JPEG quality (0-100)

        public AdminController(
            UserManager<AppUser> userManager, 
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db,
            IWebHostEnvironment webHostEnvironment,
            IEmailService emailService,
            IImageOptimizationService imageOptimizationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            this.db = db;
            this.webHostEnvironment = webHostEnvironment;
            _emailService = emailService;
            _imageOptimizationService = imageOptimizationService;
        }

        // Admin pages should NOT be cached (sensitive data)
        // GET: Admin Panel - Properties Management
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index(string statusFilter = null, int page = 1, int pageSize = 12)
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

            // Pass all properties to ViewBag for statistics
            ViewBag.AllProperties = allPropertiesForStats;
            ViewBag.CurrentFilter = statusFilter;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            return View(properties);
        }

        private void SeedLocations()
        {
            var locations = new List<Location>
            {
                new Location { 
                    Name = "Dubai Marina", 
                    City = "Dubai",
                    ImageUrl = "https://images.unsplash.com/photo-1539650116574-75c0c6d73b6e?w=1200&auto=format&fit=crop&q=80"
                },
                new Location { 
                    Name = "Downtown Dubai", 
                    City = "Dubai",
                    ImageUrl = "https://images.unsplash.com/photo-1546410531-bb4caa6b424d?w=1200&auto=format&fit=crop&q=80"
                },
                new Location { 
                    Name = "Palm Jumeirah", 
                    City = "Dubai",
                    ImageUrl = "https://images.unsplash.com/photo-1601823984263-b87b59798b70?w=1200&auto=format&fit=crop&q=80"
                },
                new Location { 
                    Name = "Business Bay", 
                    City = "Dubai",
                    ImageUrl = "https://images.unsplash.com/photo-1582407947304-fd86f028f716?w=1200&auto=format&fit=crop&q=80"
                },
                new Location { 
                    Name = "JBR (Jumeirah Beach Residence)", 
                    City = "Dubai",
                    ImageUrl = "https://images.unsplash.com/photo-1564501049412-61c2a3083791?w=1200&auto=format&fit=crop&q=80"
                },
                new Location { 
                    Name = "Dubai Hills", 
                    City = "Dubai",
                    ImageUrl = "https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=1200&auto=format&fit=crop&q=80"
                },
                new Location { 
                    Name = "Dubai Creek Harbour", 
                    City = "Dubai",
                    ImageUrl = "https://images.unsplash.com/photo-1506898667547-42e22a46e125?w=1200&auto=format&fit=crop&q=80"
                }
            };

            db.Locations.AddRange(locations);
            db.SaveChanges();
        }

        // GET: Create Property
        public IActionResult Create()
        {
            // Seed initial locations if none exist
            if (!db.Locations.Any())
            {
                SeedLocations();
            }

            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(PropertyStatus)), PropertyStatus.Available);
            
            var model = new InputPropertyModel();
            return View(model);
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
                // Find or create location by name
                var location = await db.Locations
                    .FirstOrDefaultAsync(l => l.Name.ToLower() == model.LocationName.Trim().ToLower());
                
                if (location == null)
                {
                    // Create new location if it doesn't exist
                    location = new Location
                    {
                        Name = model.LocationName.Trim(),
                        City = "Dubai" // Default to Dubai for now
                    };
                    db.Locations.Add(location);
                    await db.SaveChangesAsync();
                }

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
                    LocationId = location.Id,
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
                                    // Optimize and save image with WebP support
                                    using (var stream = imageFile.OpenReadStream())
                                    {
                                        var result = await _imageOptimizationService.OptimizeAndSaveImageWithWebPAndFallbackAsync(
                                            stream,
                                            uniqueFileName,
                                            imagesFolder,
                                            MAX_IMAGE_WIDTH,
                                            MAX_IMAGE_HEIGHT,
                                            IMAGE_QUALITY
                                        );
                                        
                                        uploadedFilePaths.Add(Path.Combine(imagesFolder, result.OriginalFileName)); // Track for cleanup
                                        uploadedFilePaths.Add(Path.Combine(imagesFolder, result.WebpFileName)); // Track WebP too

                                        var propertyImage = new PropertyImage
                                        {
                                            PropertyId = property.Id,
                                            ImageUrl = result.OriginalPath, // Store original path, WebP will be used via <picture> tag
                                            IsMain = imageIndex == mainImageIndex,
                                            CreatedAt = DateTime.Now
                                        };

                                        // Store WebP path in a separate field or use it in view
                                        // For now, we'll use the original path and add WebP support in views
                                        db.PropertyImages.Add(propertyImage);
                                    }
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
                        ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(PropertyStatus)), model.Status);
                        return View(model);
                    }
                }

                TempData["Success"] = "Property added successfully!";
                return RedirectToAction(nameof(Index));
            }

            // Reload ViewBag data if model is invalid
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(PropertyStatus)), model.Status);
            return View(model);
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
                LocationName = property.Location?.Name ?? "",
                Address = property.Address,
                Status = property.Status,
                IsActive = property.IsActive
            };

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
                // Find or create location by name
                var location = await db.Locations
                    .FirstOrDefaultAsync(l => l.Name.ToLower() == model.LocationName.Trim().ToLower());
                
                if (location == null)
                {
                    // Create new location if it doesn't exist
                    location = new Location
                    {
                        Name = model.LocationName.Trim(),
                        City = "Dubai" // Default to Dubai for now
                    };
                    db.Locations.Add(location);
                    await db.SaveChangesAsync();
                }

                // Update property fields
                property.Title = model.Title;
                property.Description = model.Description;
                property.PricePerMonth = model.PricePerMonth;
                property.Bedrooms = model.Bedrooms;
                property.Bathrooms = model.Bathrooms;
                property.SquareMeters = model.SquareMeters;
                property.LocationId = location.Id;
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

                            // Optimize and save image with WebP support
                            using (var stream = imageFile.OpenReadStream())
                            {
                                var result = await _imageOptimizationService.OptimizeAndSaveImageWithWebPAndFallbackAsync(
                                    stream,
                                    uniqueFileName,
                                    imagesFolder,
                                    MAX_IMAGE_WIDTH,
                                    MAX_IMAGE_HEIGHT,
                                    IMAGE_QUALITY
                                );

                                var propertyImage = new PropertyImage
                                {
                                    PropertyId = property.Id,
                                    ImageUrl = result.OriginalPath, // Store original path, WebP will be used via <picture> tag
                                    IsMain = false, // New images are never main - user must select from existing or newly uploaded
                                    CreatedAt = DateTime.Now
                                };

                                db.PropertyImages.Add(propertyImage);
                            }
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
                return RedirectToAction(nameof(Index));
            }

            // Reload ViewBag data if model is invalid
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
                return RedirectToAction(nameof(Index));
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

            return RedirectToAction(nameof(Index));
        }

        // GET: Viewing Requests
        public IActionResult ViewingRequests(string statusFilter = null)
        {
            var query = db.ViewingRequests
                .Include(vr => vr.Property)
                    .ThenInclude(p => p.Location)
                .Include(vr => vr.Property)
                    .ThenInclude(p => p.Images)
                .AsQueryable();

            // Apply status filter if provided
            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<ViewingRequestStatus>(statusFilter, true, out var status))
            {
                query = query.Where(vr => vr.Status == status);
            }

            var requests = query
                .OrderByDescending(vr => vr.CreatedAt)
                .ToList();

            ViewBag.StatusFilter = statusFilter;
            ViewBag.AllRequests = db.ViewingRequests.ToList(); // For statistics

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
            var newStatus = (ViewingRequestStatus)status;
            
            // If approving a request, cancel all other pending requests for the same property
            if (newStatus == ViewingRequestStatus.Approved && oldStatus != ViewingRequestStatus.Approved)
            {
                var otherRequests = await db.ViewingRequests
                    .Where(vr => vr.PropertyId == request.PropertyId 
                        && vr.Id != request.Id 
                        && vr.Status == ViewingRequestStatus.Pending)
                    .ToListAsync();
                
                foreach (var otherRequest in otherRequests)
                {
                    otherRequest.Status = ViewingRequestStatus.Cancelled;
                    otherRequest.UpdatedAt = DateTime.Now;
                    
                    // Send cancellation email to other users
                    try
                    {
                        await _emailService.SendViewingRequestStatusUpdateEmailAsync(
                            otherRequest.Email,
                            otherRequest.FullName,
                            otherRequest.Property?.Title ?? "Property",
                            otherRequest.PreferredDate,
                            otherRequest.PreferredTime,
                            ViewingRequestStatus.Cancelled.ToString()
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't break the flow
                        // Email failure shouldn't prevent cancellation
                    }
                }
            }
            
            request.Status = newStatus;
            request.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();

            // Send email notification if status changed
            if (oldStatus != request.Status)
            {
                try
                {
                    var statusName = request.Status.ToString();
                    await _emailService.SendViewingRequestStatusUpdateEmailAsync(
                        request.Email,
                        request.FullName,
                        request.Property?.Title ?? "Property",
                        request.PreferredDate,
                        request.PreferredTime,
                        statusName
                    );
                }
                catch (Exception ex)
                {
                    // Log error but don't break the flow
                    // Email failure shouldn't prevent status update
                    TempData["Warning"] = $"Viewing request status updated to {request.Status}, but email notification could not be sent.";
                }
            }

            TempData["Success"] = "Viewing request status updated successfully!";
            return RedirectToAction(nameof(ViewingRequests));
        }

        // GET: User Management
        public async Task<IActionResult> Users(string searchTerm = null)
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                users = users.Where(u => 
                    u.Email.Contains(searchTerm) || 
                    u.UserName.Contains(searchTerm));
            }

            var userList = new List<UserRoleViewModel>();

            foreach (var user in users.ToList())
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles.ToList(),
                    EmailConfirmed = user.EmailConfirmed,
                    IsDefaultAdmin = user.Email.Equals(DEFAULT_ADMIN_EMAIL, StringComparison.OrdinalIgnoreCase)
                });
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            return View(userList);
        }

        // POST: Assign Role to User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction(nameof(Users));
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    TempData["Success"] = $"Role '{roleName}' assigned to {user.Email} successfully!";
                }
                else
                {
                    TempData["Error"] = $"Failed to assign role: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            else
            {
                TempData["Warning"] = $"User {user.Email} already has the role '{roleName}'.";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: Remove Role from User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // Prevent removing Admin role from default admin
            if (user.Email.Equals(DEFAULT_ADMIN_EMAIL, StringComparison.OrdinalIgnoreCase) && 
                roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Cannot remove Admin role from the default administrator account.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    TempData["Success"] = $"Role '{roleName}' removed from {user.Email} successfully!";
                }
                else
                {
                    TempData["Error"] = $"Failed to remove role: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            else
            {
                TempData["Warning"] = $"User {user.Email} does not have the role '{roleName}'.";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: Delete User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // Prevent deleting default admin
            if (user.Email.Equals(DEFAULT_ADMIN_EMAIL, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Cannot delete the default administrator account.";
                return RedirectToAction(nameof(Users));
            }

            // Prevent deleting yourself
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"User {user.Email} deleted successfully!";
            }
            else
            {
                TempData["Error"] = $"Failed to delete user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: Messages
        public async Task<IActionResult> Messages()
        {
            var messages = await db.Messages
                .Include(m => m.Property)
                    .ThenInclude(p => p.Location)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(messages);
        }

        // POST: Delete Message
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await db.Messages.FindAsync(id);
            if (message == null)
            {
                TempData["Error"] = "Message not found.";
                return RedirectToAction(nameof(Messages));
            }

            db.Messages.Remove(message);
            await db.SaveChangesAsync();

            TempData["Success"] = "Message deleted successfully!";
            return RedirectToAction(nameof(Messages));
        }
    }

    public class UserRoleViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsDefaultAdmin { get; set; }
    }
}

