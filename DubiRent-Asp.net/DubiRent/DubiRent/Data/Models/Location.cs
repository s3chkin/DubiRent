using System.ComponentModel.DataAnnotations;

namespace DubiRent.Data.Models
{
    public class Location
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(100)]
        public string City { get; set; }

        // Image URL for location (used in Popular Locations section)
        public string? ImageUrl { get; set; }

        // Връзка към имоти
        public ICollection<Property> Properties { get; set; }
    }
}
