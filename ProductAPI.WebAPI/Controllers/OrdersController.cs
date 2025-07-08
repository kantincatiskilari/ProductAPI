using Microsoft.AspNetCore.Mvc;
using ProductAPI.Business.DTOs.Order;
using ProductAPI.Business.Services.Interfaces;
using ProductAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductAPI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// Get all orders
        /// </summary>
        /// <returns>List of orders</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrderListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderListDto>>> GetOrders()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }

      

        /// <summary>
        /// Get orders with simple pagination
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <returns>Paginated list of orders</returns>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetOrdersPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var (orders, totalCount) = await _orderService.GetOrdersPagedAsync(pageNumber, pageSize);

                var response = new
                {
                    Data = orders,
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
                _logger.LogError(ex, "Error retrieving paged orders");
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>Order details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid order ID");
                }

                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", id);
                return StatusCode(500, "An error occurred while retrieving the order");
            }
        }

        /// <summary>
        /// Get order with items
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>Order with order items</returns>
        [HttpGet("{id}/with-items")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderDto>> GetOrderWithItems(int id)
        {
            try
            {
                var order = await _orderService.GetOrderWithItemsAsync(id);
                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order with items {OrderId}", id);
                return StatusCode(500, "An error occurred while retrieving the order with items");
            }
        }

        /// <summary>
        /// Get order by order number
        /// </summary>
        /// <param name="orderNumber">Order number</param>
        /// <returns>Order details</returns>
        [HttpGet("by-number/{orderNumber}")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderDto>> GetOrderByNumber(string orderNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderNumber))
                {
                    return BadRequest("Order number is required");
                }

                var order = await _orderService.GetOrderByOrderNumberAsync(orderNumber);
                if (order == null)
                {
                    return NotFound($"Order with number {orderNumber} not found");
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order by number {OrderNumber}", orderNumber);
                return StatusCode(500, "An error occurred while retrieving the order");
            }
        }

        /// <summary>
        /// Create a new order
        /// </summary>
        /// <param name="createOrderDto">Order creation data</param>
        /// <returns>Created order</returns>
        [HttpPost]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Additional validation
                if (!createOrderDto.IsValid())
                {
                    return BadRequest("Order validation failed. Please check order items and amounts.");
                }

                var order = await _orderService.CreateOrderAsync(createOrderDto);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating order");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for user {UserId}", createOrderDto.UserId);
                return StatusCode(500, "An error occurred while creating the order");
            }
        }

        /// <summary>
        /// Update order
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="updateOrderDto">Order update data</param>
        /// <returns>Updated order</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<OrderDto>> UpdateOrder(int id, [FromBody] UpdateOrderDto updateOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if order can be modified
                if (!await _orderService.CanOrderBeModifiedAsync(id))
                {
                    return Conflict("Order cannot be modified in its current status");
                }

                var order = await _orderService.UpdateOrderAsync(id, updateOrderDto);
                if (order == null)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating order {OrderId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", id);
                return StatusCode(500, "An error occurred while updating the order");
            }
        }

        /// <summary>
        /// Delete order
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>No content if successful</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                if (!await _orderService.CanOrderBeCancelledAsync(id))
                {
                    return Conflict("Order cannot be deleted due to its current status");
                }

                var result = await _orderService.DeleteOrderAsync(id);
                if (!result)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
                return StatusCode(500, "An error occurred while deleting the order");
            }
        }

        /// <summary>
        /// Update order status
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="updateStatusDto">Status update data</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/update-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto updateStatusDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _orderService.UpdateOrderStatusAsync(id, updateStatusDto);
                if (!result)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(new { message = $"Order status updated to {updateStatusDto.Status} successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for order {OrderId}", id);
                return StatusCode(500, "An error occurred while updating order status");
            }
        }

        /// <summary>
        /// Cancel order
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="reason">Cancellation reason</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelOrder(int id, [FromQuery] string? reason = null)
        {
            try
            {
                if (!await _orderService.CanOrderBeCancelledAsync(id))
                {
                    return BadRequest("Order cannot be cancelled in its current status");
                }

                var result = await _orderService.CancelOrderAsync(id, reason);
                if (!result)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(new { message = "Order cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                return StatusCode(500, "An error occurred while cancelling the order");
            }
        }

        /// <summary>
        /// Mark order as shipped
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="notes">Optional notes</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/mark-shipped")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsShipped(int id, [FromQuery] string? notes = null)
        {
            try
            {
                var result = await _orderService.MarkAsShippedAsync(id, notes);
                if (!result)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(new { message = "Order marked as shipped successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking order as shipped {OrderId}", id);
                return StatusCode(500, "An error occurred while marking order as shipped");
            }
        }

        /// <summary>
        /// Mark order as delivered
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="notes">Optional notes</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/mark-delivered")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsDelivered(int id, [FromQuery] string? notes = null)
        {
            try
            {
                var result = await _orderService.MarkAsDeliveredAsync(id, notes);
                if (!result)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(new { message = "Order marked as delivered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking order as delivered {OrderId}", id);
                return StatusCode(500, "An error occurred while marking order as delivered");
            }
        }

        /// <summary>
        /// Process order (move from Pending to Processing)
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/process")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ProcessOrder(int id)
        {
            try
            {
                var result = await _orderService.ProcessOrderAsync(id);
                if (!result)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(new { message = "Order processing started successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order {OrderId}", id);
                return StatusCode(500, "An error occurred while processing the order");
            }
        }

        /// <summary>
        /// Get orders by user ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="takeCount">Number of recent orders to return</param>
        /// <returns>User's orders</returns>
        [HttpGet("by-user/{userId}")]
        [ProducesResponseType(typeof(IEnumerable<OrderListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderListDto>>> GetOrdersByUser(int userId, [FromQuery] int? takeCount = null)
        {
            try
            {
                IEnumerable<OrderListDto> orders;

                if (takeCount.HasValue)
                {
                    orders = await _orderService.GetOrdersByUserIdAsync(userId, takeCount.Value);
                }
                else
                {
                    orders = await _orderService.GetOrdersByUserIdAsync(userId);
                }

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user orders");
            }
        }

        /// <summary>
        /// Get user orders with pagination
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated user orders</returns>
        [HttpGet("by-user/{userId}/paged")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetUserOrdersPaged(int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var (orders, totalCount) = await _orderService.GetUserOrdersPagedAsync(userId, pageNumber, pageSize);

                var response = new
                {
                    Data = orders,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasNextPage = pageNumber * pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1,
                    UserId = userId
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged orders for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user orders");
            }
        }

        /// <summary>
        /// Get orders by status
        /// </summary>
        /// <param name="status">Order status</param>
        /// <returns>Orders with specified status</returns>
        [HttpGet("by-status/{status}")]
        [ProducesResponseType(typeof(IEnumerable<OrderListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderListDto>>> GetOrdersByStatus(OrderStatus status)
        {
            try
            {
                var orders = await _orderService.GetOrdersByStatusAsync(status);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders by status {Status}", status);
                return StatusCode(500, "An error occurred while retrieving orders by status");
            }
        }

        /// <summary>
        /// Search orders
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <returns>Search results</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<OrderListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderListDto>>> SearchOrders([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("Search term is required");
                }

                var orders = await _orderService.SearchOrdersAsync(searchTerm);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching orders with term {SearchTerm}", searchTerm);
                return StatusCode(500, "An error occurred while searching orders");
            }
        }

        /// <summary>
        /// Get orders by date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Orders in date range</returns>
        [HttpGet("by-date-range")]
        [ProducesResponseType(typeof(IEnumerable<OrderListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderListDto>>> GetOrdersByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    return BadRequest("Start date cannot be greater than end date");
                }

                var orders = await _orderService.GetOrdersByDateRangeAsync(startDate, endDate);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders by date range {StartDate} to {EndDate}", startDate, endDate);
                return StatusCode(500, "An error occurred while retrieving orders by date range");
            }
        }

        /// <summary>
        /// Get recent orders
        /// </summary>
        /// <param name="takeCount">Number of orders to return</param>
        /// <returns>Recent orders</returns>
        [HttpGet("recent")]
        [ProducesResponseType(typeof(IEnumerable<OrderListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderListDto>>> GetRecentOrders([FromQuery] int takeCount = 10)
        {
            try
            {
                if (takeCount < 1 || takeCount > 100)
                {
                    takeCount = 10;
                }

                var orders = await _orderService.GetRecentOrdersAsync(takeCount);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent orders");
                return StatusCode(500, "An error occurred while retrieving recent orders");
            }
        }

        /// <summary>
        /// Get order statistics
        /// </summary>
        /// <returns>Order summary statistics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(OrderSummaryDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<OrderSummaryDto>> GetOrderStatistics()
        {
            try
            {
                var summary = await _orderService.GetOrderSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order statistics");
                return StatusCode(500, "An error occurred while retrieving order statistics");
            }
        }

        /// <summary>
        /// Get order statistics by date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Order statistics for date range</returns>
        [HttpGet("statistics/by-date-range")]
        [ProducesResponseType(typeof(OrderSummaryDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<OrderSummaryDto>> GetOrderStatisticsByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    return BadRequest("Start date cannot be greater than end date");
                }

                var summary = await _orderService.GetOrderSummaryByDateRangeAsync(startDate, endDate);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order statistics by date range");
                return StatusCode(500, "An error occurred while retrieving order statistics");
            }
        }

        /// <summary>
        /// Get top orders by value
        /// </summary>
        /// <param name="takeCount">Number of orders to return</param>
        /// <returns>Top orders by value</returns>
        [HttpGet("top-by-value")]
        [ProducesResponseType(typeof(IEnumerable<OrderListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderListDto>>> GetTopOrdersByValue([FromQuery] int takeCount = 10)
        {
            try
            {
                if (takeCount < 1 || takeCount > 100)
                {
                    takeCount = 10;
                }

                var orders = await _orderService.GetTopOrdersByValueAsync(takeCount);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top orders by value");
                return StatusCode(500, "An error occurred while retrieving top orders");
            }
        }

        /// <summary>
        /// Get revenue by date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Revenue for date range</returns>
        [HttpGet("revenue")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetRevenue(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                decimal revenue;

                if (startDate.HasValue && endDate.HasValue)
                {
                    if (startDate > endDate)
                    {
                        return BadRequest("Start date cannot be greater than end date");
                    }
                    revenue = await _orderService.GetRevenueByDateRangeAsync(startDate.Value, endDate.Value);
                }
                else
                {
                    revenue = await _orderService.GetTotalRevenueAsync();
                }

                var response = new
                {
                    Revenue = revenue,
                    FormattedRevenue = $"${revenue:N2}",
                    StartDate = startDate,
                    EndDate = endDate,
                    IsTotal = !startDate.HasValue && !endDate.HasValue
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revenue");
                return StatusCode(500, "An error occurred while retrieving revenue");
            }
        }

        /// <summary>
        /// Add item to order
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="orderItemDto">Order item data</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/items")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddOrderItem(int id, [FromBody] CreateOrderItemDto orderItemDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (!await _orderService.CanOrderBeModifiedAsync(id))
                {
                    return Conflict("Order cannot be modified in its current status");
                }

                var result = await _orderService.AddOrderItemAsync(id, orderItemDto);
                if (!result)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(new { message = "Order item added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to order {OrderId}", id);
                return StatusCode(500, "An error occurred while adding item to order");
            }
        }

        /// <summary>
        /// Update order item
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="itemId">Order item ID</param>
        /// <param name="orderItemDto">Order item update data</param>
        /// <returns>Success status</returns>
        [HttpPut("{id}/items/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateOrderItem(int id, int itemId, [FromBody] CreateOrderItemDto orderItemDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (!await _orderService.CanOrderBeModifiedAsync(id))
                {
                    return Conflict("Order cannot be modified in its current status");
                }

                var result = await _orderService.UpdateOrderItemAsync(id, itemId, orderItemDto);
                if (!result)
                {
                    return NotFound($"Order with ID {id} or item with ID {itemId} not found");
                }

                return Ok(new { message = "Order item updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {ItemId} in order {OrderId}", itemId, id);
                return StatusCode(500, "An error occurred while updating order item");
            }
        }

        /// <summary>
        /// Remove item from order
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="itemId">Order item ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}/items/{itemId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RemoveOrderItem(int id, int itemId)
        {
            try
            {
                if (!await _orderService.CanOrderBeModifiedAsync(id))
                {
                    return Conflict("Order cannot be modified in its current status");
                }

                var result = await _orderService.RemoveOrderItemAsync(id, itemId);
                if (!result)
                {
                    return NotFound($"Order with ID {id} or item with ID {itemId} not found");
                }

                return Ok(new { message = "Order item removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item {ItemId} from order {OrderId}", itemId, id);
                return StatusCode(500, "An error occurred while removing order item");
            }
        }

        /// <summary>
        /// Recalculate order total
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/recalculate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RecalculateOrderTotal(int id)
        {
            try
            {
                var result = await _orderService.RecalculateOrderTotalAsync(id);
                if (!result)
                {
                    return NotFound($"Order with ID {id} not found");
                }

                return Ok(new { message = "Order total recalculated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating total for order {OrderId}", id);
                return StatusCode(500, "An error occurred while recalculating order total");
            }
        }

        /// <summary>
        /// Bulk update order status
        /// </summary>
        /// <param name="orderIds">List of order IDs</param>
        /// <param name="newStatus">New status</param>
        /// <param name="notes">Optional notes</param>
        /// <returns>Success status</returns>
        [HttpPost("bulk-update-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkUpdateOrderStatus(
            [FromBody] IEnumerable<int> orderIds,
            [FromQuery] OrderStatus newStatus,
            [FromQuery] string? notes = null)
        {
            try
            {
                if (!orderIds.Any())
                {
                    return BadRequest("Order IDs are required");
                }

                var result = await _orderService.BulkUpdateOrderStatusAsync(orderIds, newStatus, notes);
                if (!result)
                {
                    return BadRequest("Failed to update order statuses");
                }

                return Ok(new { message = $"{orderIds.Count()} orders updated to {newStatus} successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk order status update");
                return StatusCode(500, "An error occurred while updating order statuses");
            }
        }

        /// <summary>
        /// Bulk cancel orders
        /// </summary>
        /// <param name="orderIds">List of order IDs</param>
        /// <param name="reason">Cancellation reason</param>
        /// <returns>Success status</returns>
        [HttpPost("bulk-cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkCancelOrders(
            [FromBody] IEnumerable<int> orderIds,
            [FromQuery] string? reason = null)
        {
            try
            {
                if (!orderIds.Any())
                {
                    return BadRequest("Order IDs are required");
                }

                var result = await _orderService.BulkCancelOrdersAsync(orderIds, reason);
                if (!result)
                {
                    return BadRequest("Failed to cancel orders");
                }

                return Ok(new { message = $"Bulk cancellation completed for {orderIds.Count()} orders" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk order cancellation");
                return StatusCode(500, "An error occurred while cancelling orders");
            }
        }

        /// <summary>
        /// Get daily revenue analytics
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Daily revenue data</returns>
        [HttpGet("analytics/daily-revenue")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetDailyRevenue(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    return BadRequest("Start date cannot be greater than end date");
                }

                var dailyRevenue = await _orderService.GetDailyRevenueAsync(startDate, endDate);

                var response = new
                {
                    DailyRevenue = dailyRevenue,
                    TotalRevenue = dailyRevenue.Values.Sum(),
                    AverageDailyRevenue = dailyRevenue.Values.Any() ? dailyRevenue.Values.Average() : 0,
                    StartDate = startDate,
                    EndDate = endDate,
                    DaysCount = dailyRevenue.Count
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving daily revenue analytics");
                return StatusCode(500, "An error occurred while retrieving daily revenue");
            }
        }

        /// <summary>
        /// Get daily order count analytics
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Daily order count data</returns>
        [HttpGet("analytics/daily-orders")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetDailyOrderCount(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    return BadRequest("Start date cannot be greater than end date");
                }

                var dailyOrderCount = await _orderService.GetDailyOrderCountAsync(startDate, endDate);

                var response = new
                {
                    DailyOrderCount = dailyOrderCount,
                    TotalOrders = dailyOrderCount.Values.Sum(),
                    AverageDailyOrders = dailyOrderCount.Values.Any() ? dailyOrderCount.Values.Average() : 0,
                    StartDate = startDate,
                    EndDate = endDate,
                    DaysCount = dailyOrderCount.Count
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving daily order count analytics");
                return StatusCode(500, "An error occurred while retrieving daily order count");
            }
        }

        /// <summary>
        /// Get top customers by order value
        /// </summary>
        /// <param name="takeCount">Number of customers to return</param>
        /// <returns>Top customers</returns>
        [HttpGet("analytics/top-customers")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetTopCustomers([FromQuery] int takeCount = 10)
        {
            try
            {
                if (takeCount < 1 || takeCount > 100)
                {
                    takeCount = 10;
                }

                var topCustomers = await _orderService.GetTopCustomersByOrderValueAsync(takeCount);

                var response = new
                {
                    TopCustomers = topCustomers.Select(kvp => new
                    {
                        UserId = kvp.Key,
                        TotalOrderValue = kvp.Value,
                        FormattedValue = $"${kvp.Value:N2}"
                    }),
                    Count = topCustomers.Count,
                    TotalValue = topCustomers.Values.Sum()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top customers analytics");
                return StatusCode(500, "An error occurred while retrieving top customers");
            }
        }

        /// <summary>
        /// Get order calculation preview
        /// </summary>
        /// <param name="createOrderDto">Order data for calculation</param>
        /// <returns>Order calculations</returns>
        [HttpPost("calculate")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> CalculateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var subtotal = await _orderService.CalculateOrderSubtotalAsync(createOrderDto);
                var tax = await _orderService.CalculateOrderTaxAsync(createOrderDto);
                var discount = await _orderService.CalculateOrderDiscountAsync(createOrderDto);
                var shipping = await _orderService.CalculateShippingCostAsync(createOrderDto);
                var total = await _orderService.CalculateOrderTotalAsync(createOrderDto);

                var response = new
                {
                    Subtotal = subtotal,
                    Tax = tax,
                    Discount = discount,
                    Shipping = shipping,
                    Total = total,
                    FormattedAmounts = new
                    {
                        Subtotal = $"${subtotal:N2}",
                        Tax = $"${tax:N2}",
                        Discount = $"${discount:N2}",
                        Shipping = $"${shipping:N2}",
                        Total = $"${total:N2}"
                    },
                    ItemsCount = createOrderDto.OrderItems.Count(),
                    TaxRate = "10%",
                    FreeShippingThreshold = "$100.00"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating order");
                return StatusCode(500, "An error occurred while calculating order");
            }
        }

        /// <summary>
        /// Validate order without creating
        /// </summary>
        /// <param name="createOrderDto">Order data for validation</param>
        /// <returns>Validation results</returns>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ValidateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var isValidOrder = await _orderService.ValidateOrderAsync(createOrderDto);
                var hasStock = await _orderService.ValidateStockAvailabilityAsync(createOrderDto);
                var isValidDto = createOrderDto.IsValid();

                var response = new
                {
                    IsValid = isValidOrder && hasStock && isValidDto,
                    ValidationResults = new
                    {
                        OrderValidation = isValidOrder,
                        StockAvailability = hasStock,
                        DtoValidation = isValidDto
                    },
                    ItemsCount = createOrderDto.OrderItems.Count(),
                    Message = (isValidOrder && hasStock && isValidDto)
                        ? "Order is valid and can be created"
                        : "Order validation failed. Please check the details."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating order");
                return StatusCode(500, "An error occurred while validating order");
            }
        }

        /// <summary>
        /// Get orders created today
        /// </summary>
        /// <returns>Today's orders</returns>
        [HttpGet("today")]
        [ProducesResponseType(typeof(IEnumerable<OrderListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderListDto>>> GetOrdersCreatedToday()
        {
            try
            {
                var orders = await _orderService.GetOrdersCreatedTodayAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's orders");
                return StatusCode(500, "An error occurred while retrieving today's orders");
            }
        }

        /// <summary>
        /// Get orders created this week
        /// </summary>
        /// <returns>This week's orders</returns>
        [HttpGet("this-week")]
        [ProducesResponseType(typeof(IEnumerable<OrderListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderListDto>>> GetOrdersCreatedThisWeek()
        {
            try
            {
                var orders = await _orderService.GetOrdersCreatedThisWeekAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving this week's orders");
                return StatusCode(500, "An error occurred while retrieving this week's orders");
            }
        }

        /// <summary>
        /// Get orders created this month
        /// </summary>
        /// <returns>This month's orders</returns>
        [HttpGet("this-month")]
        [ProducesResponseType(typeof(IEnumerable<OrderListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderListDto>>> GetOrdersCreatedThisMonth()
        {
            try
            {
                var orders = await _orderService.GetOrdersCreatedThisMonthAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving this month's orders");
                return StatusCode(500, "An error occurred while retrieving this month's orders");
            }
        }
    }
}