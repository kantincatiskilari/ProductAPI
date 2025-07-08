
namespace ProductAPI.Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TotalPrice { get; set; }

        //Navigation Properties
        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }

    }
}
