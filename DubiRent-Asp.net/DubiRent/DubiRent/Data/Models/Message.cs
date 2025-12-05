using System.ComponentModel.DataAnnotations;

namespace DubiRent.Data.Models
{
    public class Message
    {
        public int Id { get; set; }

        public string UserId { get; set; }  // Identity UserId, optional
        // public IdentityUser User { get; set; } // optional

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; }

        [Required, StringLength(1000)]
        public string MessageText { get; set; }

        public int? PropertyId { get; set; }
        public Property Property { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
