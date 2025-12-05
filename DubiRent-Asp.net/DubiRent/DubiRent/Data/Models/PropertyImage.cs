using System.ComponentModel.DataAnnotations;

namespace DubiRent.Data.Models
{
    public class PropertyImage
    {
        public int Id { get; set; }

        [Required]
        public int PropertyId { get; set; }
        public Property Property { get; set; }

        [Required, StringLength(500)]
        public string ImageUrl { get; set; }

        public bool IsMain { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
