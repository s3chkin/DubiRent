using System.ComponentModel.DataAnnotations;
using DubiRent.Data.Models;

namespace DubiRent.Models
{
    public class ViewingRequestModel
    {
        [Required]
        public int PropertyId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Preferred date is required")]
        [Display(Name = "Preferred Date")]
        public DateTime PreferredDate { get; set; } = DateTime.Now.AddDays(1);

        [Required(ErrorMessage = "Preferred time is required")]
        [Display(Name = "Preferred Time")]
        public TimeSpan PreferredTime { get; set; } = new TimeSpan(10, 0, 0);
    }
}

