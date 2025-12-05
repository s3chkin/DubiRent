using Microsoft.AspNetCore.Identity;

namespace DubiRent.Data.Models
{
    public class AppUser : IdentityUser
    {
        public AppUser()
        {
            Roles = new HashSet<IdentityUserRole<string>>();
            Claims = new HashSet<IdentityUserClaim<string>>();
            Logins = new HashSet<IdentityUserLogin<string>>();
            this.Id = Guid.NewGuid().ToString();
        }
        public virtual ICollection<IdentityUserRole<string>> Roles { get; set; }
        public virtual ICollection<IdentityUserClaim<string>> Claims { get; set; }
        public virtual ICollection<IdentityUserLogin<string>> Logins { get; set; }
    }
}
