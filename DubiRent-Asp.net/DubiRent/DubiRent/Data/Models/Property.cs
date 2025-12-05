using System.ComponentModel.DataAnnotations;

namespace DubiRent.Data.Models
{
    public enum PropertyStatus
    {
        Available,
        Rented,
        Archived
    }

    public class Property
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public decimal PricePerMonth { get; set; }

        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int SquareMeters { get; set; }

        [Required]
        public int LocationId { get; set; }
        public Location Location { get; set; }  // връзка към Location

        [Required, StringLength(500)]
        public string Address { get; set; }

        public bool IsActive { get; set; } = true;

        public PropertyStatus Status { get; set; } = PropertyStatus.Available;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Връзки
        public ICollection<PropertyImage> Images { get; set; }
        public ICollection<ViewingRequest> ViewingRequests { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public ICollection<Favourite> Favourites { get; set; }
        public ICollection<Message> Messages { get; set; }
    }
}
