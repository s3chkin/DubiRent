using DubiRent.Data;
using DubiRent.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
