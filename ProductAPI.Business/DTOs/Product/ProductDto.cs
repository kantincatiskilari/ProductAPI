using System.ComponentModel.DataAnnotations;

namespace ProductAPI.Business.DTOs.Product
{
    //Read Dto  
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? SKU { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        //Calculated Properties
        public bool IsInStock => StockQuantity > 0;
        public bool IsLowStock => StockQuantity > 0 && StockQuantity <= 10;
        public bool IsOutOfStock => StockQuantity == 0;
        public string StockStatus => GetStockStatus();
        public string FormattedPrice => $"${Price:F2}";

        public string GetStockStatus()
        {
            return StockQuantity switch
            {
                0 => "Out of Stock",
                <= 10 => "Low Stock",
                _ => "In Stock"
            };
        }
    }
    //Create Dto
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        [MaxLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        public string Name { get; set; }

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        [RegularExpression(@"^[A-Z0-9-_]+$", ErrorMessage = "SKU can only contain uppercase letters, numbers, hyphens, and underscores")]
        public string? SKU { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01 ,double.MaxValue, ErrorMessage = "Price must be greater than zero")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be greater than zero")]
        public int StockQuantity { get; set; }

        public bool IsActive { get; set; } = true;
    }
    //Update Dto
    public class UpdateProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        [MaxLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        public string Name { get; set; }

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        [RegularExpression(@"^[A-Z0-9-_]+$", ErrorMessage = "SKU can only contain uppercase letters, numbers, hyphens, and underscores")]
        public string? SKU { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be greater than zero")]
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
    }

    //List Dto
    public class ProductListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? SKU { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Display properties
        public string FormattedPrice => $"${Price:F2}";
        public string StockStatus => GetStockStatus();
        public string StatusDisplayName => IsActive ? "Active" : "Inactive";
        public bool IsInStock => StockQuantity > 0;

        private string GetStockStatus()
        {
            return StockQuantity switch
            {
                0 => "Out of Stock",
                <= 10 => "Low Stock",
                _ => "In Stock"
            };
        }
    }

    // Summary DTO (for dashboard)
    public class ProductSummaryDto
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int InStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal AveragePrice { get; set; }
    }

    //Stock Update Dto
    public class UpdateStockDto
    {
        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public StockUpdateType UpdateType { get; set; } = StockUpdateType.Set;
    }

    // Stock Movement DTO(for inventory history)
    public class StockMovementDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }
        public int QuantityChanged { get; set; }
        public StockUpdateType UpdateType { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        // Display properties
        public string UpdateTypeDisplayName => UpdateType.ToString();
        public string QuantityChangeDisplay => QuantityChanged >= 0 ? $"+{QuantityChanged}" : QuantityChanged.ToString();
    }

    // Filter DTO
    public class ProductFilterDto
    {
        public string? Name { get; set; }
        public string? SKU { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsInStock { get; set; }
        public bool? IsLowStock { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinStock { get; set; }
        public int? MaxStock { get; set; }

        // Paging
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Sorting
        public string SortBy { get; set; } = "Name";
        public bool SortDescending { get; set; } = false;
    }

    // Price Update DTO (for bulk price changes)
    public class UpdatePriceDto
    {
        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public PriceUpdateType UpdateType { get; set; } = PriceUpdateType.Set;

        // For percentage-based updates
        [Range(-100, 1000, ErrorMessage = "Percentage must be between -100 and 1000")]
        public decimal? Percentage { get; set; }
    }

    // Bulk operation DTOs
    public class BulkUpdateProductsDto
    {
        [Required(ErrorMessage = "Product IDs are required")]
        [MinLength(1, ErrorMessage = "At least one product ID is required")]
        public ICollection<int> ProductIds { get; set; } = new List<int>();

        public bool? IsActive { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
    }

}
