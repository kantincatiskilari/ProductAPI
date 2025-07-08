using ProductAPI.Business.DTOs.User;
using ProductAPI.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProductAPI.Business.DTOs.Order
{
    // Read DTO
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public OrderStatus Status { get; set; }
        public string? Notes { get; set; }
        public string? ShippingAddress { get; set; }

        //Navigation Properties
        public UserListDto? User { get; set; }
        public ICollection<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();

        //Calculated Properties
        public decimal FinalAmount => TotalAmount - (DiscountAmount ?? 0);
        public string StatusDisplayName => Status.ToString();
        public int ItemsCount => OrderItems?.Count ?? 0;
        public bool IsShipped => ShippedDate != null;
        public bool IsDelivered => DeliveredDate != null;
        public bool CanBeCancelled => Status == OrderStatus.Pending || Status == OrderStatus.Processing;
    }

    //Create Dto
    public class CreateOrderDto
    {
        [Required(ErrorMessage = "User ID is required.")]
        public int UserId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
        public decimal TotalAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount amount can not be negative")]
        public decimal? DiscountAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tax amount cannot be negative")]
        public decimal? TaxAmount { get; set; }

        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
        [Required(ErrorMessage = "Order items are required")]
        [MinLength(1, ErrorMessage = "At least one order item is required")]
        public ICollection<CreateOrderItemDto> OrderItems { get; set; } = new List<CreateOrderItemDto>();

        [MaxLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? ShippingAddress { get; set; }

        // Validation method
        public bool IsValid()
        {
            if (DiscountAmount.HasValue && DiscountAmount > TotalAmount)
                return false;

            return OrderItems.Any() && OrderItems.All(item => item.IsValid());
        }
    }

    //Update Dto
    public class UpdateOrderDto
    {
        public OrderStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
        [StringLength(1000, ErrorMessage = "Shipping address cannot exceed 1000 characters")]
        public string? ShippingAddress { get; set; }

        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
    }

    //List Dto
    public class OrderListDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public int ItemsCount { get; set; }

        // Display properties
        public decimal FinalAmount => TotalAmount - (DiscountAmount ?? 0);
        public string StatusDisplayName => Status.ToString();
        public string OrderDateDisplay => OrderDate.ToString("dd/MM/yyyy HH:mm");
    }

    //Summary Dto
    public class OrderSummaryDto
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public Dictionary<OrderStatus, int> OrdersByStatus { get; set; } = new();

    }

    // Order Status Update DTO
    public class UpdateOrderStatusDto
    {
        [Required(ErrorMessage = "Status is required")]
        public OrderStatus Status { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        // Auto-set dates based on status
        public DateTime? GetStatusDate()
        {
            return Status switch
            {
                OrderStatus.Shipped => DateTime.UtcNow,
                OrderStatus.Delivered => DateTime.UtcNow,
                _ => null
            };
        }
    }
    // Filter DTO
    public class OrderFilterDto
    {
        public int? UserId { get; set; }
        public OrderStatus? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string? OrderNumber { get; set; }

        // Paging
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Sorting
        public string SortBy { get; set; } = "OrderDate";
        public bool SortDescending { get; set; } = true;
    }

}
