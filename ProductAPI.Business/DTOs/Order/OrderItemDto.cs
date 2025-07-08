using System.ComponentModel.DataAnnotations;

namespace ProductAPI.Business.DTOs.Order
{
    public class OrderItemDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string? ProductDescription { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }

        // Calculated properties
        public decimal FinalPrice => TotalPrice - (DiscountAmount ?? 0);
        public decimal UnitPriceAfterDiscount => FinalPrice / Quantity;
    }

    public class CreateOrderItemDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        public string ProductName { get; set; }

        [StringLength(1000, ErrorMessage = "Product description cannot exceed 1000 characters")]
        public string? ProductDescription { get; set; }

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount amount cannot be negative")]
        public decimal? DiscountAmount { get; set; }

        // Calculated property
        public decimal TotalPrice => (UnitPrice * Quantity) - (DiscountAmount ?? 0);

        public bool IsValid()
        {
            return UnitPrice > 0 && Quantity > 0 &&
                   (!DiscountAmount.HasValue || DiscountAmount >= 0) &&
                   (!DiscountAmount.HasValue || DiscountAmount <= (UnitPrice * Quantity));
        }
    }
}
