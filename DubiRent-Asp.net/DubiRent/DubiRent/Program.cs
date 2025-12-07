using DubiRent.Data;
using DubiRent.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

namespace DubiRent
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            builder.Services.AddControllersWithViews();

            // Performance Optimizations
            // Response Compression (gzip/brotli)
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
                options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
                options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/json", "text/css", "application/javascript", "text/html", "application/xml", "text/xml", "application/xhtml+xml" }
                );
            });

            // Configure Compression Provider Options
            builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Optimal;
            });

            builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Optimal;
            });

            // Response Caching
            builder.Services.AddResponseCaching();

            // Output Caching (for .NET 8+)
            builder.Services.AddOutputCache(options =>
            {
                // Default cache policy
                options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(10);
                options.SizeLimit = 100; // Maximum number of cached responses
                
                // Don't cache responses for authenticated users
                options.AddPolicy("NoAuth", builder => builder.NoCache());
            });

            // Static File Caching Configuration will be done in UseStaticFiles middleware

            // Register Email Service
            builder.Services.AddScoped<DubiRent.Services.IEmailService, DubiRent.Services.EmailService>();
            
            // Register Image Optimization Service
            builder.Services.AddScoped<DubiRent.Services.IImageOptimizationService, DubiRent.Services.ImageOptimizationService>();
            
            // Register Stripe Service
            builder.Services.AddScoped<DubiRent.Services.IStripeService, DubiRent.Services.StripeService>();

            // External Authentication
            var authBuilder = builder.Services.AddAuthentication();
            
            // Configure Google Authentication
            IConfigurationSection googleAuthSection = builder.Configuration.GetSection("Authentication:Google");
            var googleClientId = googleAuthSection["ClientId"];
            var googleClientSecret = googleAuthSection["ClientSecret"];
            
            if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
            {
                authBuilder.AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                    // Configure callback path explicitly
                    options.CallbackPath = "/Identity/Account/ExternalLogin/Callback";
                });
            }
            
            // Configure Facebook Authentication (only if credentials are provided)
            IConfigurationSection facebookAuthSection = builder.Configuration.GetSection("Authentication:Facebook");
            var facebookAppId = facebookAuthSection["AppId"];
            var facebookAppSecret = facebookAuthSection["AppSecret"];
            
            if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret) && 
                facebookAppId != "YOUR_FACEBOOK_APP_ID" && facebookAppSecret != "YOUR_FACEBOOK_APP_SECRET")
            {
                authBuilder.AddFacebook(options =>
                {
                    options.AppId = facebookAppId;
                    options.AppSecret = facebookAppSecret;
                    options.CallbackPath = "/Identity/Account/ExternalLogin/Callback";
                });
            }

            var app = builder.Build();

            // Seed roles and admin user
            using (var scope = app.Services.CreateScope())
            {
                await SeedRoles.SeedAsync(scope.ServiceProvider);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Performance Middleware (order matters!)
            app.UseResponseCompression(); // Must be before UseStaticFiles and UseRouting
            app.UseResponseCaching(); // Must be before UseStaticFiles and UseRouting
            
            // Static files with caching headers
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    // Cache static files for 1 year (immutable files like CSS, JS, images)
                    var path = ctx.File.Name.ToLowerInvariant();
                    if (path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".png") || 
                        path.EndsWith(".jpg") || path.EndsWith(".jpeg") || path.EndsWith(".gif") || 
                        path.EndsWith(".svg") || path.EndsWith(".webp") || path.EndsWith(".woff") || 
                        path.EndsWith(".woff2") || path.EndsWith(".ttf") || path.EndsWith(".eot"))
                    {
                        ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                    }
                    // Cache other static files for 1 day
                    else
                    {
                        ctx.Context.Response.Headers.CacheControl = "public, max-age=86400";
                    }
                }
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Output Cache Middleware (must be after UseRouting and UseAuthorization)
            app.UseOutputCache();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
