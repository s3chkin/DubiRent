using DubiRent.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DubiRent.Data
{
    public class ApplicationDbContext :  IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            this.Database.Migrate();
            UpdateDescriptionColumn();
            UpdateUserIdColumn();
            UpdateIpAddressColumn();
        }
        
        private void UpdateDescriptionColumn()
        {
            try
            {
                // Check if Description column is nvarchar(1000) and update to nvarchar(max)
                var sql = @"
                    IF EXISTS (
                        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Properties' 
                        AND COLUMN_NAME = 'Description' 
                        AND CHARACTER_MAXIMUM_LENGTH = 1000
                    )
                    BEGIN
                        ALTER TABLE [Properties]
                        ALTER COLUMN [Description] nvarchar(MAX) NOT NULL;
                    END";
                
                this.Database.ExecuteSqlRaw(sql);
            }
            catch
            {
                // Silently fail - migration might already be applied or table doesn't exist yet
            }
        }

        private void UpdateUserIdColumn()
        {
            try
            {
                // Make UserId nullable in ViewingRequests table if it's currently not nullable
                var sql = @"
                    IF EXISTS (
                        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'ViewingRequests' 
                        AND COLUMN_NAME = 'UserId' 
                        AND IS_NULLABLE = 'NO'
                    )
                    BEGIN
                        ALTER TABLE [ViewingRequests]
                        ALTER COLUMN [UserId] nvarchar(MAX) NULL;
                    END";
                
                this.Database.ExecuteSqlRaw(sql);
            }
            catch
            {
                // Silently fail - migration might already be applied or table doesn't exist yet
            }
        }

        private void UpdateIpAddressColumn()
        {
            try
            {
                // Add IpAddress column to ViewingRequests table if it doesn't exist
                var sql = @"
                    IF NOT EXISTS (
                        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'ViewingRequests' 
                        AND COLUMN_NAME = 'IpAddress'
                    )
                    BEGIN
                        ALTER TABLE [ViewingRequests]
                        ADD [IpAddress] nvarchar(45) NULL;
                    END";
                
                this.Database.ExecuteSqlRaw(sql);
            }
            catch
            {
                // Silently fail - migration might already be applied or table doesn't exist yet
            }
        }
        
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<ViewingRequest> ViewingRequests { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Favourite> Favourites { get; set; }
        public DbSet<Message> Messages { get; set; }

    }
}
