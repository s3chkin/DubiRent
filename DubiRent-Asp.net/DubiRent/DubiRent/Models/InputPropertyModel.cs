using System.ComponentModel.DataAnnotations;
using DubiRent.Data.Models;
using Microsoft.AspNetCore.Http;

namespace DubiRent.Models
{
    public class InputPropertyModel
    {
        public int? Id { get; set; } // For editing existing properties

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price per month is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal PricePerMonth { get; set; }

        [Required(ErrorMessage = "Number of bedrooms is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Bedrooms must be 0 or greater")]
        public int Bedrooms { get; set; }

        [Required(ErrorMessage = "Number of bathrooms is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Bathrooms must be 0 or greater")]
        public int Bathrooms { get; set; }

        [Required(ErrorMessage = "Square meters is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Square meters must be greater than 0")]
        public int SquareMeters { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public int LocationId { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; }

        public PropertyStatus Status { get; set; } = PropertyStatus.Available;

        // Images - multiple file uploads
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();

        // Optional: IsActive flag
        public bool IsActive { get; set; } = true;
    }
}
