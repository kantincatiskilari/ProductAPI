namespace ProductAPI.Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 1,
        Processing,
        Shipped,
        Delivered,
        Cancelled,
        Returned,
        Refunded
    }
}
