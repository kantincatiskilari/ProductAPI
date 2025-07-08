using ProductAPI.Business.DTOs.Product;


namespace ProductAPI.Business.Services.Interfaces
{
    public interface IProductService
    {
        //Basic CRUD Operations
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<IEnumerable<ProductListDto>> GetAllProductsAsync();
        Task<ProductDto?> CreateProductAsync(CreateProductDto createProductDto);
        Task<ProductDto?> UpdateProductAsync(int id,UpdateProductDto updateProductDto);
        Task<bool> DeleteProductAsync(int id);

        // Product Search & Filtering

        Task<IEnumerable<ProductListDto>> SearchProductsAsync(string searchTerm);
        Task<IEnumerable<ProductListDto>> GetActiveProductsAsync();
        Task<IEnumerable<ProductListDto>> GetInactiveProductsAsync();
        Task<IEnumerable<ProductListDto>> GetInStockProductsAsync();
        Task<IEnumerable<ProductListDto>> GetOutOfStockProductsAsync();
        Task<IEnumerable<ProductListDto>> GetLowStockProductsAsync();

        //Advanced filtering w/ pagination
        Task<(IEnumerable<ProductListDto>, int TotalCount)> GetProductsPagedAsync(int pageNumber, int pageSize);
  

        // SKU Operations

        Task<ProductDto?> GetProductBySkuAsync(string sku);
        Task<bool> IsSkuAvailableAsync(string sku);
        Task<bool> IsSkuAvailableAsync(string sku, int excludeProductId);

        // Stock Management

        Task<bool> UpdateStockAsync(int productId, UpdateStockDto updateStockDto);
        Task<bool> AddStockAsync(int productId, int quantity, string? notes = null);
        Task<bool> RemoveStockAsync(int productId, int quantity, string? notes = null);
        Task<IEnumerable<ProductListDto>> GetProductsByStockRangeAsync(int minStock, int maxStock);

        // Price Management

        Task<bool> UpdatePriceAsync(int productId, UpdatePriceDto updatePriceDto);
        Task<bool> BulkUpdatePricesAsync(IEnumerable<int> productIds, UpdatePriceDto updatePriceDto);
        Task<IEnumerable<ProductListDto>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);

        // Status Management

        Task<bool> ActivateProductStatusAsync(int productId);
        Task<bool> DeactivateProductStatusAsync(int productId);
        Task<bool> ToggleProductStatusAsync(int productId);
        Task<IEnumerable<int>> BulkActivateProductsAsync(IEnumerable<int> productIds);
        Task<IEnumerable<int>> BulkDeactivateProductsAsync(IEnumerable<int> productIds);

        // Statistics & Analytics

        Task<ProductSummaryDto> GetProductSummaryAsync();
        Task<int> GetTotalProductsCountAsync();
        Task<int> GetActiveProductsCountAsync();
        Task<int> GetInStockProductsCountAsync();
        Task<int> GetOutOfStockProductsCountAsync();
        Task<int> GetLowStockProductsCountAsync();
        Task<decimal> GetTotalInventoryValueAsync();
        Task<decimal> GetAverageProductPriceAsync();

        // Inventory Reports

        Task<IEnumerable<ProductListDto>> GetTopSellingProductsAsync(int takeCount = 10);
        Task<IEnumerable<ProductListDto>> GetRecentlyAddedProductsAsync(int takeCount = 10);
        Task<IEnumerable<ProductListDto>> GetProductsAddedBetweenAsync(DateTime start, DateTime end);
        Task<IEnumerable<ProductListDto>> GetProductsAddedTodayAsync();
        Task<IEnumerable<ProductListDto>> GetProductsAddedThisWeekAsync();
        Task<IEnumerable<ProductListDto>> GetProductsAddedThisMonthAsync();

        // Bulk Operations

        Task<IEnumerable<ProductListDto>> GetProductsByIdsAsync(IEnumerable<int> productIds);
        Task<bool> BulkUpdateProductsAsync(BulkUpdateProductsDto bulkUpdateProductsDto);
        Task<bool> BulkDeleteProductsAsync(IEnumerable<int> productIds);

        // Validation & Business Roles

        Task<bool> ProductExistsAsync(int productId);
        Task<bool> CanProductBeDeletedAsync(int productId);
        Task<bool> HasSufficientStockAsync(int productId, int requiredQuantity);
        Task<bool> CanStockBeReducedAsync(int productId, int quantity);

        // Advanced Queries

        Task<IEnumerable<ProductListDto>> GetSimilarProductsAsync(int productId, int takeCount = 5);
        Task<IEnumerable<ProductListDto>> GetRecommendedProductsAsync(int userId, int takeCount = 10);
        Task<IEnumerable<ProductListDto>> GetFeaturedProductsAsync();

        // Import/Export Operations
        Task<byte[]> ExportProductsToExcelAsync();
        Task<byte[]> ExportProductsToExcelAsync(bool? isActive = null, bool? isInStock = null);
        Task<string> ExportProductsToCsvAsync();
        Task<bool> ImportProductsFromCsvAsync(byte[] csvData);

        // Stock Movement History (if implementing stock tracking)
        Task<IEnumerable<StockMovementDto>> GetStockMovementHistoryAsync(int productId);
        Task<IEnumerable<StockMovementDto>> GetRecentStockMovementsAsync(int takeCount = 20);


    }
}
