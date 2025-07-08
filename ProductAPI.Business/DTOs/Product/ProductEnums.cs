namespace ProductAPI.Business.DTOs.Product
{
    public enum StockUpdateType
    {
        Set = 1,      // Set exact quantity
        Add = 2,      // Add to current quantity
        Subtract = 3  // Subtract from current quantity
    }

    public enum PriceUpdateType
    {
        Set = 1,           // Set exact price
        Percentage = 2,    // Apply percentage change
        FixedAmount = 3    // Add/subtract fixed amount
    }
}
