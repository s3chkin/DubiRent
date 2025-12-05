using System.ComponentModel.DataAnnotations;

namespace DubiRent.Data.Models
{
    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }  // Identity UserId

        [Required]
        public int PropertyId { get; set; }
        public Property Property { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required, StringLength(10)]
        public string Currency { get; set; } = "AED";

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [StringLength(50)]
        public string PaymentProvider { get; set; }

        [StringLength(100)]
        public string TransactionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
