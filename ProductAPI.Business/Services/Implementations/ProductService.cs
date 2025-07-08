using AutoMapper;
using Microsoft.Extensions.Logging;
using ProductAPI.Business.DTOs.Product;
using ProductAPI.Business.Services.Interfaces;
using ProductAPI.DataAccess.UnitOfWork;
using ProductAPI.Domain.Entities;
using System.Linq.Expressions;

namespace ProductAPI.Business.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ProductService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        // Basic CRUD Operations

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if (product == null)
                {
                    _logger.LogInformation("Product id: {ProductId} not found", id);
                    return null;
                }

                return _mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured when getting product by id : {ProductId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetAllProductsAsync()
        {
            try
            {
                var products = await _unitOfWork.Products.GetAllAsync();
                if (products == null)
                {
                    _logger.LogInformation("Products not found");
                    return null;
                }

                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured when getting products");
                throw;
            }
        }

        public async Task<ProductDto?> CreateProductAsync(CreateProductDto createProductDto)
        {
            try
            {
                // SKU Validation
                if(!string.IsNullOrEmpty(createProductDto.SKU) && !await IsSkuAvailableAsync(createProductDto.SKU))
                {
                    throw new InvalidOperationException($"SKU {createProductDto.SKU} is already in use");
                    
                }

                var product = _mapper.Map<Product>(createProductDto);
                product.CreatedAt = DateTime.Now;

                await _unitOfWork.Products.AddAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Product created successfully with ID {ProductId}", product.Id);

                return _mapper.Map<ProductDto>(product);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product with name: {ProductName}", createProductDto.Name);
                throw;
            }
        }

        public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if(product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for update", id);
                    return null;
                }

                // SKU uniqueness validation (excluding current product)
                if (!string.IsNullOrEmpty(updateProductDto.SKU) && !await IsSkuAvailableAsync(updateProductDto.SKU, id))
                {
                    throw new InvalidOperationException($"SKU {updateProductDto.SKU} is already in use by another product");
                }

                _mapper.Map(updateProductDto, product);
                product.UpdatedAt = DateTime.UtcNow;

                _logger.LogWarning("Product with ID: {ProductId} updated successfully", id);
                return _mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if (product == null || !await CanProductBeDeletedAsync(id))
                {
                    _logger.LogWarning("Product with ID {ProductId} can not be deleted", id);
                    return false;
                }

                await _unitOfWork.Products.DeleteAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Product with ID {ProductId} deleted successfully", id);
                return true;

            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
                throw;
            }
        }

        // Product Search & Filtering

        public async Task<IEnumerable<ProductListDto>> SearchProductsAsync(string searchTerm)
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => p.Name.Contains(searchTerm) 
                                    || (!string.IsNullOrEmpty(p.SKU) && p.SKU.Contains(searchTerm)) 
                                    || (!string.IsNullOrEmpty(p.Description) && p.Description.Contains(searchTerm)));

                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error searching products with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetActiveProductsAsync()
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => p.IsActive);
                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when getting active products");
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetInactiveProductsAsync()
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => !p.IsActive);
                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when getting inactive products");
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetInStockProductsAsync()
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => p.StockQuantity > 0);
                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-stock products");
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetLowStockProductsAsync()
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => p.StockQuantity > 0 && p.StockQuantity < 10);
                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting in-stock products");
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetOutOfStockProductsAsync()
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => p.StockQuantity == 0);
                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting out-of-stock products");
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetProductsAddedBetweenAsync(DateTime start, DateTime end)
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => p.CreatedAt >= start && p.CreatedAt <= end);
                if(products == null)
                {
                    _logger.LogWarning("No product found added between dates {start} - {end}", start, end);
                    return null;
                }

                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occured");
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetProductsAddedThisMonthAsync()
        {
            try
            {
                DateTime start = DateTime.Now;
                DateTime end = DateTime.Now.AddDays(30);
                var products = await _unitOfWork.Products.FindAsync(p => p.CreatedAt >= start && p.CreatedAt <= end);
                if (products == null)
                {
                    _logger.LogWarning("No product found added between dates {start} - {end}", start, end);
                    return null;
                }

                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occured");
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetProductsAddedThisWeekAsync()
        {
            try
            {
                DateTime start = DateTime.Now;
                DateTime end = DateTime.Now.AddDays(7);
                var products = await _unitOfWork.Products.FindAsync(p => p.CreatedAt >= start && p.CreatedAt <= end);
                if (products == null)
                {
                    _logger.LogWarning("No product found added between dates {start} - {end}", start, end);
                    return null;
                }

                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occured");
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetProductsAddedTodayAsync()
        {
            try
            {
                DateTime start = DateTime.Now;
                DateTime end = DateTime.Now.AddDays(1);
                var products = await _unitOfWork.Products.FindAsync(p => p.CreatedAt >= start && p.CreatedAt <= end);
                if (products == null)
                {
                    _logger.LogWarning("No product found added between dates {start} - {end}", start, end);
                    return null;
                }

                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occured");
                throw;
            }
        }

        // Advanced filtering w/ pagination

        public async Task<(IEnumerable<ProductListDto>, int TotalCount)> GetProductsPagedAsync(int pageNumber, int pageSize)
        {
            try
            {
                var products = await _unitOfWork.Products.GetPagedAsync(pageNumber, pageSize);

                var totalCount = await _unitOfWork.Products.CountAsync();

                return (_mapper.Map<IEnumerable<ProductListDto>>(products), totalCount);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error getting paged products");
                throw;
            }
        }

        // SKU Operations

        public async Task<ProductDto?> GetProductBySkuAsync(string sku)
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => p.SKU == sku);
                if(products == null)
                {
                    _logger.LogWarning("Products with SKU: {SKU} not found", sku);
                    return null;
                }

                return _mapper.Map<ProductDto>(products);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex,"Error getting product by SKU {SKU}",sku);
                throw;
            }
        }

        public async Task<bool> IsSkuAvailableAsync(string sku)
        {
            try
            {
                return !await _unitOfWork.Products.ExistsAsync(p => p.SKU == sku);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking SKU availability {SKU}", sku);
                throw;
            }
        }

        public async Task<bool> IsSkuAvailableAsync(string sku, int excludeProductId)
        {
            try
            {
                return !await _unitOfWork.Products.ExistsAsync(p => p.SKU == sku && p.Id != excludeProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking SKU availability {SKU} excluding product {ProductId}", sku, excludeProductId);
                throw;
            }
        }

        // Stock Management

        public async Task<bool> UpdateStockAsync(int productId, UpdateStockDto updateStockDto)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for stock update", productId);
                    return false;
                }

                int previousStock = product.StockQuantity;
                int newStock = updateStockDto.UpdateType switch
                {
                    StockUpdateType.Set => updateStockDto.StockQuantity,
                    StockUpdateType.Add => previousStock + updateStockDto.StockQuantity,
                    StockUpdateType.Subtract => Math.Max(0, product.StockQuantity - updateStockDto.StockQuantity),
                    _                        => previousStock
                };

                if(newStock < 0)
                {
                    _logger.LogWarning("Stock update would result in negative stock for product {ProductId}", productId);
                    return false;
                }

                product.StockQuantity = newStock;
                product.UpdatedAt = DateTime.Now;

                await _unitOfWork.Products.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Stock updated for product {ProductId} from {PreviousStock} to {NewStock}",
                    productId, previousStock, newStock);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> AddStockAsync(int productId, int quantity, string? notes = null)
        {
            try
            {
               var product = await _unitOfWork.Products.GetByIdAsync(productId);
               if (product == null) return false;

                UpdateStockDto updateStockDto = new()
                {
                    StockQuantity = product.StockQuantity + quantity,
                    Notes = notes
                };

 
                return await UpdateStockAsync(productId, updateStockDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> RemoveStockAsync(int productId, int quantity, string? notes = null)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for stock removal", productId);
                    return false;
                }

                if (product.StockQuantity < quantity)
                {
                    _logger.LogInformation("Insufficient stock for product {ProductId}. Requested: {Requested}, Available: {Available}",
                                           productId, quantity, product.StockQuantity);
                    return false;
                }

                UpdateStockDto updateStockDto = new()
                {
                    StockQuantity = product.StockQuantity - quantity,
                    Notes = notes
                };

                return await UpdateStockAsync(productId, updateStockDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetProductsByStockRangeAsync(int minStock, int maxStock)
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => p.StockQuantity <= maxStock && p.StockQuantity >= minStock);

                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error getting products by stock range {MinStock}-{MaxStock}", minStock, maxStock);
                throw;
            }
        }

        // Price Management

        public async Task<bool> UpdatePriceAsync(int productId, UpdatePriceDto updatePriceDto)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("Product not found");
                    return false;
                }
                decimal previousPrice = product.Price;

                decimal newPrice = updatePriceDto.UpdateType switch
                {
                    PriceUpdateType.Set => updatePriceDto.Price,
                    PriceUpdateType.Percentage => product.Price * (1 + (updatePriceDto.Percentage ?? 0) / 100),
                    PriceUpdateType.FixedAmount => product.Price + updatePriceDto.Price,
                    _ => product.Price
                };

                if (newPrice <= 0)
                {
                    _logger.LogWarning("Price update would result in invalid price for product {ProductId}", productId);
                    return false;
                }

                product.Price = newPrice;
                product.UpdatedAt = DateTime.Now;

                await _unitOfWork.Products.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Price updated for product {ProductId} from {PreviousPrice:C} to {NewPrice:C}",
                    productId, previousPrice, newPrice);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating price for product {ProductId}", productId);
                throw;
            }
        }
        public async Task<bool> BulkUpdatePricesAsync(IEnumerable<int> productIds, UpdatePriceDto updatePriceDto)
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => productIds.Contains(p.Id));
                foreach (var product in products)
                {

                    decimal newPrice = updatePriceDto.UpdateType switch
                    {
                        PriceUpdateType.Set => updatePriceDto.Price,
                        PriceUpdateType.Percentage => product.Price * (1 + (updatePriceDto.Percentage ?? 0) / 100),
                        PriceUpdateType.FixedAmount => product.Price + updatePriceDto.Price,
                        _ => product.Price
                    };

                    product.Price = newPrice;
                    product.UpdatedAt = DateTime.Now;
                    
                }

                await _unitOfWork.Products.UpdateRangeAsync(products);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Bulk price update completed for {Count} products", products.Count());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk price update");
                throw;
            }
        }

        public async Task<IEnumerable<ProductListDto>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => p.Price <= maxPrice && p.Price >= minPrice);
                if(products == null)
                {
                    _logger.LogInformation("No product found in range {minPrice} - {maxPrice}",minPrice,maxPrice);
                    return null;
                }

                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            } catch(Exception ex)
            {
                _logger.LogInformation(ex,"Error occured when getting products");
                throw;
            }
        }

        // Status Management

        public async Task<bool> ActivateProductStatusAsync(int productId)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                {
                    return false;
                }
                product.IsActive = true;
                product.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Products.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error activated product status");
                throw;
            }
        }

        public async Task<bool> DeactivateProductStatusAsync(int productId)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                {
                    return false;
                }
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;


                await _unitOfWork.Products.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivated product status");
                throw;
            }
        }

        public async Task<bool> ToggleProductStatusAsync(int productId)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                {
                    return false;
                }
                product.IsActive = !product.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Products.UpdateAsync(product);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggle product status");
                throw;
            }
        }

        public async Task<IEnumerable<int>> BulkActivateProductsAsync(IEnumerable<int> productIds)
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => productIds.Contains(p.Id));
                if (products == null)
                {
                    return null;
                }
                foreach (var product in products)
                {
                    product.IsActive = true;
                    product.UpdatedAt = DateTime.UtcNow;
                }
                await _unitOfWork.Products.UpdateRangeAsync(products);
                await _unitOfWork.SaveChangesAsync();
                return productIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activate products status");
                throw;
            }
        }

        public async Task<IEnumerable<int>> BulkDeactivateProductsAsync(IEnumerable<int> productIds)
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => productIds.Contains(p.Id));
                if (products == null)
                {
                    return null;
                }
                foreach (var product in products)
                {
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.UtcNow;
                }
                await _unitOfWork.Products.UpdateRangeAsync(products);
                await _unitOfWork.SaveChangesAsync();
                return productIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivate products status");
                throw;
            }
        }

        // Statistics & Analytics
        public async Task<ProductSummaryDto> GetProductSummaryAsync()
        {
            try
            {
                var totalProducts = await _unitOfWork.Products.CountAsync();
                var activeProducts = await _unitOfWork.Products.CountAsync(p => p.IsActive);
                var inStockProducts = await _unitOfWork.Products.CountAsync(p => p.StockQuantity > 0);
                var outOfStockProducts = await _unitOfWork.Products.CountAsync(p => p.StockQuantity == 0);
                var lowStockProducts = await _unitOfWork.Products.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= 10);

                var allProducts = await _unitOfWork.Products.GetAllAsync();
                var totalInventoryValue = allProducts.Sum(p => p.Price * p.StockQuantity);
                var averagePrice = allProducts.Any() ? allProducts.Average(p => p.Price) : 0;

                return new ProductSummaryDto
                {
                    TotalProducts = totalProducts,
                    ActiveProducts = activeProducts,
                    InactiveProducts = totalProducts - activeProducts,
                    InStockProducts = inStockProducts,
                    OutOfStockProducts = outOfStockProducts,
                    LowStockProducts = lowStockProducts,
                    TotalInventoryValue = totalInventoryValue,
                    AveragePrice = averagePrice
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product summary");
                throw;
            }
        }

        public async Task<int> GetTotalProductsCountAsync()
        {
            return await _unitOfWork.Products.CountAsync();
        }

        public async Task<int> GetActiveProductsCountAsync()
        {
            return await _unitOfWork.Products.CountAsync(p => p.IsActive);
        }

        public async Task<int> GetInStockProductsCountAsync()
        {
            return await _unitOfWork.Products.CountAsync(p => p.StockQuantity > 0);
        }

        public async Task<int> GetOutOfStockProductsCountAsync()
        {
            return await _unitOfWork.Products.CountAsync(p => p.StockQuantity == 0);
        }

        public async Task<int> GetLowStockProductsCountAsync()
        {
            return await _unitOfWork.Products.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= 10);
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            try
            {
                var products = await _unitOfWork.Products.GetAllAsync();
                return products.Sum(p => p.Price * p.StockQuantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total inventory value");
                throw;
            }
        }

        public async Task<decimal> GetAverageProductPriceAsync()
        {
            try
            {
                var products = await _unitOfWork.Products.GetAllAsync();
                return products.Any() ? products.Average(p => p.Price) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average product price");
                throw;
            }
        }

        // Inventory Reports

        public async Task<IEnumerable<ProductListDto>> GetTopSellingProductsAsync(int takeCount = 10)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ProductListDto>> GetRecentlyAddedProductsAsync(int takeCount = 10)
        {
            try
            {
                var products = await _unitOfWork.Products.GetOrderedAsync(
                    p => p.CreatedAt,
                    false); // Descending

                var recentProducts = products.Take(takeCount);
                return _mapper.Map<IEnumerable<ProductListDto>>(recentProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recently added products");
                throw;
            }
        }

        // Bulk Operations
        public async Task<IEnumerable<ProductListDto>> GetProductsByIdsAsync(IEnumerable<int> productIds)
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => productIds.Contains(p.Id));

                if(products == null)
                {
                    return null;
                }

                return _mapper.Map<IEnumerable<ProductListDto>>(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error occured when getting products");
                throw;
            }
        }

        public async Task<bool> BulkDeleteProductsAsync(IEnumerable<int> productIds)
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => productIds.Contains(p.Id));

                if (products == null)
                {
                    _logger.LogWarning("Products not found");
                    return false;
                }

                await _unitOfWork.Products.DeleteRangeAsync(products);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured when deleting products");
                throw;
            }
        }

        public async Task<bool> BulkUpdateProductsAsync(BulkUpdateProductsDto bulkUpdateProductsDto)
        {
            try
            {
                var products = await _unitOfWork.Products.FindAsync(p => bulkUpdateProductsDto.ProductIds.Contains(p.Id));
                if (products == null)
                {
                    return false;
                }
                foreach (var product in products)
                {
                    if (bulkUpdateProductsDto.IsActive.HasValue)
                        product.IsActive = bulkUpdateProductsDto.IsActive.Value;
                    if (bulkUpdateProductsDto.Price.HasValue)
                        product.Price = bulkUpdateProductsDto.Price.Value;
                    if (bulkUpdateProductsDto.StockQuantity.HasValue)
                        product.StockQuantity = bulkUpdateProductsDto.StockQuantity.Value;
                }
                await _unitOfWork.Products.UpdateRangeAsync(products);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Products updated successfully");
                return true;
                }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured when updating products");
                throw;
            }
        }

        // Validation & Business Roles

        public async Task<bool> ProductExistsAsync(int productId)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
                return false;

              return true;
        }

        public Task<bool> CanProductBeDeletedAsync(int productId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> CanStockBeReducedAsync(int productId, int quantity)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if(product == null)
                {
                    _logger.LogWarning("Product with ID: {ProductId} not found",productId);
                    return false;
                }

                if (product.StockQuantity >= quantity)
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if stock can be reduced for product {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> HasSufficientStockAsync(int productId, int requiredQuantity)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("Product with ID: {ProductId} not found", productId);
                    return false;
                }

                if (product.StockQuantity >= requiredQuantity)
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if stock can be reduced for product {ProductId}", productId);
                throw;
            }
        }


        // Advanced Queries

        public async Task<IEnumerable<ProductListDto>> GetSimilarProductsAsync(int productId, int takeCount = 5)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("Product with ID: {ProductId} not found", productId);
                    return Enumerable.Empty<ProductListDto>();
                }

                // Price similarity
                decimal minPrice = product.Price * 0.6m;
                decimal maxPrice = product.Price * 1.4m;

                var similarProducts = await _unitOfWork.Products.FindAsync(p => p.Id != productId 
                    && p.IsActive 
                    && p.Price >= minPrice 
                    && p.Price <= maxPrice);

                var result = similarProducts.Take(takeCount);

                return _mapper.Map<IEnumerable<ProductListDto>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured");
                throw;
            }
        }

        public Task<IEnumerable<ProductListDto>> GetRecommendedProductsAsync(int userId, int takeCount = 10)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ProductListDto>> GetFeaturedProductsAsync()
        {
            throw new NotImplementedException();
        }

        // Import/Export Operations

        public Task<string> ExportProductsToCsvAsync()
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ExportProductsToExcelAsync()
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ExportProductsToExcelAsync(bool? isActive = null, bool? isInStock = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ImportProductsFromCsvAsync(byte[] csvData)
        {
            throw new NotImplementedException();
        }


        // Stock Movement History (if implementing stock tracking)


        public Task<IEnumerable<StockMovementDto>> GetRecentStockMovementsAsync(int takeCount = 20)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<StockMovementDto>> GetStockMovementHistoryAsync(int productId)
        {
            throw new NotImplementedException();
        }

       

       
      
 
    }
}
