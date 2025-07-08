using ProductAPI.Domain.Enums;

namespace ProductAPI.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string? Notes { get; set; }
        public string? ShippingAddress { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
