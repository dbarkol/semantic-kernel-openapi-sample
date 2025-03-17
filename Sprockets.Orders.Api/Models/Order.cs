
using System.ComponentModel.DataAnnotations;

namespace Sprockets.Orders.Api.Models
{
    public class Order
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Product { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000.")]
        public int Quantity { get; set; }
    }
}