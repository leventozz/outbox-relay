using System.ComponentModel.DataAnnotations;

namespace OutboxRelay.Api.Models
{
    public class CreateTransactionRequest
    {
        [Required(ErrorMessage = "FromAccountId is required")]
        public int FromAccountId { get; set; }

        [Required(ErrorMessage = "ToAccountId is required")]
        public int ToAccountId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }
}
