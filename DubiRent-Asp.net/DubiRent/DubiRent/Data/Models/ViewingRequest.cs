using System.ComponentModel.DataAnnotations;

namespace DubiRent.Data.Models
{
    public enum ViewingRequestStatus
    {
        Pending,
        Approved,
        Completed,
        Cancelled
    }

    public class ViewingRequest
    {
        public int Id { get; set; }

        [Required]
        public int PropertyId { get; set; }
        public Property Property { get; set; }

        public string UserId { get; set; }  // Identity UserId
        // public IdentityUser User { get; set; }  // optional връзка към AspNetUsers

        [Required]
        [StringLength(200)]
        public string FullName { get; set; }

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; }

        [Required]
        public DateTime PreferredDate { get; set; }

        [Required]
        public TimeSpan PreferredTime { get; set; }

        public ViewingRequestStatus Status { get; set; } = ViewingRequestStatus.Pending;

        [StringLength(45)] // IPv6 max length
        public string? IpAddress { get; set; } // Store IP for spam protection

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
