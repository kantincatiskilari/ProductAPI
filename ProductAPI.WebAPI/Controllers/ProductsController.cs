using Microsoft.AspNetCore.Mvc;
using ProductAPI.Business.DTOs.Product;
using ProductAPI.Business.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductAPI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Get all products
        /// </summary>
        /// <returns>List of products</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProducts()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, "An error occurred while retrieving products");
            }
        }


        /// <summary>
        /// Get products with simple pagination
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <returns>Paginated list of products</returns>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetProductsPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var (products, totalCount) = await _productService.GetProductsPagedAsync(pageNumber, pageSize);

                var response = new
                {
                    Data = products,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasNextPage = pageNumber * pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged products");
                return StatusCode(500, "An error occurred while retrieving products");
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid product ID");
                }

                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                return StatusCode(500, "An error occurred while retrieving the product");
            }
        }

        /// <summary>
        /// Get product by SKU
        /// </summary>
        /// <param name="sku">Product SKU</param>
        /// <returns>Product details</returns>
        [HttpGet("by-sku/{sku}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> GetProductBySku(string sku)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sku))
                {
                    return BadRequest("SKU is required");
                }

                var product = await _productService.GetProductBySkuAsync(sku);
                if (product == null)
                {
                    return NotFound($"Product with SKU {sku} not found");
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by SKU {SKU}", sku);
                return StatusCode(500, "An error occurred while retrieving the product");
            }
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        /// <param name="createProductDto">Product creation data</param>
        /// <returns>Created product</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check SKU availability if provided
                if (!string.IsNullOrEmpty(createProductDto.SKU) && !await _productService.IsSkuAvailableAsync(createProductDto.SKU))
                {
                    return BadRequest($"SKU {createProductDto.SKU} is already in use");
                }

                var product = await _productService.CreateProductAsync(createProductDto);
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating product");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, "An error occurred while creating the product");
            }
        }

        /// <summary>
        /// Update product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="updateProductDto">Product update data</param>
        /// <returns>Updated product</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check SKU availability if provided (excluding current product)
                if (!string.IsNullOrEmpty(updateProductDto.SKU) && !await _productService.IsSkuAvailableAsync(updateProductDto.SKU, id))
                {
                    return BadRequest($"SKU {updateProductDto.SKU} is already in use by another product");
                }

                var product = await _productService.UpdateProductAsync(id, updateProductDto);
                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                return Ok(product);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating product {ProductId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, "An error occurred while updating the product");
            }
        }

        /// <summary>
        /// Delete product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>No content if successful</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                if (!await _productService.ProductExistsAsync(id))
                {
                    return NotFound($"Product with ID {id} not found");
                }

                if (!await _productService.CanProductBeDeletedAsync(id))
                {
                    return Conflict("Product cannot be deleted due to existing orders or other dependencies");
                }

                var result = await _productService.DeleteProductAsync(id);
                if (!result)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, "An error occurred while deleting the product");
            }
        }

        /// <summary>
        /// Search products
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <returns>Search results</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> SearchProducts([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("Search term is required");
                }

                var products = await _productService.SearchProductsAsync(searchTerm);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with term {SearchTerm}", searchTerm);
                return StatusCode(500, "An error occurred while searching products");
            }
        }

        /// <summary>
        /// Get active products
        /// </summary>
        /// <returns>Active products</returns>
        [HttpGet("active")]
        [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> GetActiveProducts()
        {
            try
            {
                var products = await _productService.GetActiveProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active products");
                return StatusCode(500, "An error occurred while retrieving active products");
            }
        }

        /// <summary>
        /// Get in-stock products
        /// </summary>
        /// <returns>In-stock products</returns>
        [HttpGet("in-stock")]
        [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> GetInStockProducts()
        {
            try
            {
                var products = await _productService.GetInStockProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving in-stock products");
                return StatusCode(500, "An error occurred while retrieving in-stock products");
            }
        }

        /// <summary>
        /// Get out-of-stock products
        /// </summary>
        /// <returns>Out-of-stock products</returns>
        [HttpGet("out-of-stock")]
        [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> GetOutOfStockProducts()
        {
            try
            {
                var products = await _productService.GetOutOfStockProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving out-of-stock products");
                return StatusCode(500, "An error occurred while retrieving out-of-stock products");
            }
        }

        /// <summary>
        /// Get low-stock products
        /// </summary>
        /// <returns>Low-stock products</returns>
        [HttpGet("low-stock")]
        [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> GetLowStockProducts()
        {
            try
            {
                var products = await _productService.GetLowStockProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving low-stock products");
                return StatusCode(500, "An error occurred while retrieving low-stock products");
            }
        }

        /// <summary>
        /// Get products by price range
        /// </summary>
        /// <param name="minPrice">Minimum price</param>
        /// <param name="maxPrice">Maximum price</param>
        /// <returns>Products in price range</returns>
        [HttpGet("price-range")]
        [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProductsByPriceRange(
            [FromQuery] decimal minPrice,
            [FromQuery] decimal maxPrice)
        {
            try
            {
                if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
                {
                    return BadRequest("Invalid price range");
                }

                var products = await _productService.GetProductsByPriceRangeAsync(minPrice, maxPrice);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products by price range {MinPrice}-{MaxPrice}", minPrice, maxPrice);
                return StatusCode(500, "An error occurred while retrieving products by price range");
            }
        }

        /// <summary>
        /// Update product stock
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="updateStockDto">Stock update data</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/update-stock")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto updateStockDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _productService.UpdateStockAsync(id, updateStockDto);
                if (!result)
                {
                    return NotFound($"Product with ID {id} not found or invalid stock update");
                }

                return Ok(new { message = "Stock updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", id);
                return StatusCode(500, "An error occurred while updating stock");
            }
        }

        /// <summary>
        /// Add stock to product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="quantity">Quantity to add</param>
        /// <param name="notes">Optional notes</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/add-stock")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddStock(int id, [FromQuery] int quantity, [FromQuery] string? notes = null)
        {
            try
            {
                if (quantity <= 0)
                {
                    return BadRequest("Quantity must be greater than 0");
                }

                var result = await _productService.AddStockAsync(id, quantity, notes);
                if (!result)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                return Ok(new { message = $"Added {quantity} units to stock successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding stock to product {ProductId}", id);
                return StatusCode(500, "An error occurred while adding stock");
            }
        }

        /// <summary>
        /// Remove stock from product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="quantity">Quantity to remove</param>
        /// <param name="notes">Optional notes</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/remove-stock")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveStock(int id, [FromQuery] int quantity, [FromQuery] string? notes = null)
        {
            try
            {
                if (quantity <= 0)
                {
                    return BadRequest("Quantity must be greater than 0");
                }

                var result = await _productService.RemoveStockAsync(id, quantity, notes);
                if (!result)
                {
                    return NotFound($"Product with ID {id} not found or insufficient stock");
                }

                return Ok(new { message = $"Removed {quantity} units from stock successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing stock from product {ProductId}", id);
                return StatusCode(500, "An error occurred while removing stock");
            }
        }

        /// <summary>
        /// Update product price
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="updatePriceDto">Price update data</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/update-price")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePrice(int id, [FromBody] UpdatePriceDto updatePriceDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _productService.UpdatePriceAsync(id, updatePriceDto);
                if (!result)
                {
                    return NotFound($"Product with ID {id} not found or invalid price update");
                }

                return Ok(new { message = "Price updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating price for product {ProductId}", id);
                return StatusCode(500, "An error occurred while updating price");
            }
        }

        /// <summary>
        /// Activate product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateProduct(int id)
        {
            try
            {
                var result = await _productService.ActivateProductStatusAsync(id);
                if (!result)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                return Ok(new { message = "Product activated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating product {ProductId}", id);
                return StatusCode(500, "An error occurred while activating the product");
            }
        }

        /// <summary>
        /// Deactivate product
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateProduct(int id)
        {
            try
            {
                var result = await _productService.DeactivateProductStatusAsync(id);
                if (!result)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                return Ok(new { message = "Product deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating product {ProductId}", id);
                return StatusCode(500, "An error occurred while deactivating the product");
            }
        }

        /// <summary>
        /// Get similar products
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="takeCount">Number of similar products to return</param>
        /// <returns>Similar products</returns>
        [HttpGet("{id}/similar")]
        [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> GetSimilarProducts(int id, [FromQuery] int takeCount = 5)
        {
            try
            {
                if (!await _productService.ProductExistsAsync(id))
                {
                    return NotFound($"Product with ID {id} not found");
                }

                if (takeCount < 1 || takeCount > 20)
                {
                    takeCount = 5;
                }

                var products = await _productService.GetSimilarProductsAsync(id, takeCount);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving similar products for {ProductId}", id);
                return StatusCode(500, "An error occurred while retrieving similar products");
            }
        }

        /// <summary>
        /// Get product statistics
        /// </summary>
        /// <returns>Product summary statistics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ProductSummaryDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProductSummaryDto>> GetProductStatistics()
        {
            try
            {
                var summary = await _productService.GetProductSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product statistics");
                return StatusCode(500, "An error occurred while retrieving product statistics");
            }
        }

        /// <summary>
        /// Get recently added products
        /// </summary>
        /// <param name="takeCount">Number of products to return</param>
        /// <returns>Recently added products</returns>
        [HttpGet("recent")]
        [ProducesResponseType(typeof(IEnumerable<ProductListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductListDto>>> GetRecentProducts([FromQuery] int takeCount = 10)
        {
            try
            {
                if (takeCount < 1 || takeCount > 100)
                {
                    takeCount = 10;
                }

                var products = await _productService.GetRecentlyAddedProductsAsync(takeCount);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent products");
                return StatusCode(500, "An error occurred while retrieving recent products");
            }
        }

        /// <summary>
        /// Bulk activate products
        /// </summary>
        /// <param name="productIds">List of product IDs</param>
        /// <returns>Success status</returns>
        [HttpPost("bulk-activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkActivateProducts([FromBody] IEnumerable<int> productIds)
        {
            try
            {
                if (!productIds.Any())
                {
                    return BadRequest("Product IDs are required");
                }

                var activatedProductIds = await _productService.BulkActivateProductsAsync(productIds);
                return Ok(new { message = $"{activatedProductIds.Count()} products activated successfully", productIds = activatedProductIds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk product activation");
                return StatusCode(500, "An error occurred while activating products");
            }
        }

        /// <summary>
        /// Bulk deactivate products
        /// </summary>
        /// <param name="productIds">List of product IDs</param>
        /// <returns>Success status</returns>
        [HttpPost("bulk-deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkDeactivateProducts([FromBody] IEnumerable<int> productIds)
        {
            try
            {
                if (!productIds.Any())
                {
                    return BadRequest("Product IDs are required");
                }

                var deactivatedProductIds = await _productService.BulkDeactivateProductsAsync(productIds);
                return Ok(new { message = $"{deactivatedProductIds.Count()} products deactivated successfully", productIds = deactivatedProductIds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk product deactivation");
                return StatusCode(500, "An error occurred while deactivating products");
            }
        }

        /// <summary>
        /// Bulk update product prices
        /// </summary>
        /// <param name="productIds">List of product IDs</param>
        /// <param name="updatePriceDto">Price update data</param>
        /// <returns>Success status</returns>
        [HttpPost("bulk-update-prices")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkUpdatePrices([FromQuery] IEnumerable<int> productIds, [FromBody] UpdatePriceDto updatePriceDto)
        {
            try
            {
                if (!productIds.Any())
                {
                    return BadRequest("Product IDs are required");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _productService.BulkUpdatePricesAsync(productIds, updatePriceDto);
                if (!result)
                {
                    return BadRequest("Failed to update prices");
                }

                return Ok(new { message = $"Prices updated successfully for {productIds.Count()} products" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk price update");
                return StatusCode(500, "An error occurred while updating prices");
            }
        }
    }
}