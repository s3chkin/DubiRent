using System.ComponentModel.DataAnnotations;

namespace DubiRent.Data.Models
{
    public class Favourite
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }  // Identity UserId

        [Required]
        public int PropertyId { get; set; }
        public Property Property { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
