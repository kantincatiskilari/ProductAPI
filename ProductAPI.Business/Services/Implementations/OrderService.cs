using AutoMapper;
using Microsoft.Extensions.Logging;
using ProductAPI.Business.DTOs.Order;
using ProductAPI.Business.Services.Interfaces;
using ProductAPI.DataAccess.UnitOfWork;
using ProductAPI.Domain.Entities;
using ProductAPI.Domain.Enums;
using System.Linq.Expressions;
using static Azure.Core.HttpHeader;

namespace ProductAPI.Business.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<OrderService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        // Basic CRUD Operations
        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(id);

                if (order == null)
                {
                    _logger.LogWarning("Order with ID : {orderId} not found", id);
                    return null;
                }
                return _mapper.Map<OrderDto>(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured");
                throw;
            }
        }

        public async Task<OrderDto?> GetOrderWithItemsAsync(int id)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(id,
                    o => o.OrderItems, 
                    oi => oi.OrderItems.Select(oi => oi.Product), 
                    o => o.User);

                if (order == null)
                {
                    _logger.LogWarning("Order with items ID {OrderId} not found", id);
                    return null;
                }

                return _mapper.Map<OrderDto>(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order with items by ID {OrderId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<OrderListDto>> GetAllOrdersAsync()
        {
            try
            {
                var orders = await _unitOfWork.Orders.GetAllAsync();

                if(orders == null)
                {
                    _logger.LogWarning("No product found");
                    return null;
                }

                return _mapper.Map<IEnumerable<OrderListDto>>(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured when getting products");
                throw;
            }
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto)
        {
            try
            {
                // Validation Process
                if (!await ValidateOrderAsync(createOrderDto))
                {
                    throw new InvalidOperationException("Order validation failed");
                }

                // Check stock availability
                if (!await ValidateStockAvailabilityAsync(createOrderDto))
                {
                    throw new InvalidOperationException("Insufficient stock for one or more items");
                }

                await _unitOfWork.BeginTransactionAsync();

                // Generate order number
                var orderNumber = await GenerateOrderNumberAsync();

                // Create order entity
                var order = _mapper.Map<Order>(createOrderDto);
                order.OrderNumber = orderNumber;
                order.OrderDate = DateTime.UtcNow;
                order.Status = OrderStatus.Pending;

                // Calculate totals
                order.TotalAmount = await CalculateOrderTotalAsync(createOrderDto);

                // Create order
                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // Create order items
                foreach (var itemDto in createOrderDto.OrderItems)
                {
                    var orderItem = _mapper.Map<OrderItem>(itemDto);
                    orderItem.OrderId = order.Id;
                    orderItem.TotalPrice = itemDto.UnitPrice * itemDto.Quantity - (itemDto.DiscountAmount ?? 0);

                    await _unitOfWork.OrderItems.AddAsync(orderItem);
                }

                await _unitOfWork.SaveChangesAsync();

                // Reserve stock
                await ReserveStockForOrderAsync(createOrderDto);

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Order created successfully with ID {OrderId} and number {OrderNumber}", order.Id, orderNumber);

      

                return _mapper.Map<OrderDto>(order);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating order for user {UserId}", createOrderDto.UserId);
                throw;
            }
        }

        public async Task<OrderDto?> UpdateOrderAsync(int id, UpdateOrderDto updateOrderDto)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(id);
                if (order == null)
                {
                    _logger.LogWarning("Order with ID: {OrderID} not found", id);
                    return null;
                }

                if (!await CanOrderBeModifiedAsync(id))
                {
                    throw new InvalidOperationException("Order cannot be modified in its current status");
                }

                _mapper.Map(updateOrderDto, order);

                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Order with ID {OrderId} updated successfully", id);
                return _mapper.Map<OrderDto>(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order with ID {OrderId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(id);
                if (order == null)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found for deletion", id);
                    return false;
                }

                if (!await CanOrderBeCancelledAsync(id))
                {
                    _logger.LogWarning("Order with ID {OrderId} cannot be deleted due to business rules", id);
                    return false;
                }

                await _unitOfWork.BeginTransactionAsync();

                // Restore stock if order was processed
                await RestoreStockFromCancelledOrderAsync(id);

                // Delete order items first
                var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == id);
                await _unitOfWork.OrderItems.DeleteRangeAsync(orderItems);

                // Delete order
                await _unitOfWork.Orders.DeleteAsync(order);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Order with ID {OrderId} deleted successfully", id);
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error deleting order with ID {OrderId}", id);
                throw;
            }
        }

        // Order Number Operations

        public async Task<OrderDto?> GetOrderByOrderNumberAsync(string orderNumber)
        {
            try
            {
                var order = await _unitOfWork.Orders.FindAsync(o => o.OrderNumber == orderNumber);
                if (order == null)
                    return null;

                return _mapper.Map<OrderDto?>(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured.");
                throw;
            }
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            try
            {
                var currentYear = DateTime.UtcNow.Year;
                var currentMonth = DateTime.UtcNow.Month;

                var startOfMonth = new DateTime(currentYear, currentMonth, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                var ordersThisMonth = await _unitOfWork.Orders.CountAsync(o => o.OrderDate > startOfMonth && o.OrderDate < endOfMonth);
                var orderNumber = $"ORD-{currentYear:D4}{currentMonth:D2}-{(ordersThisMonth + 1):D4}";

                while (await _unitOfWork.Orders.ExistsAsync(o => o.OrderNumber == orderNumber))
                {
                    ordersThisMonth++;
                    orderNumber = $"ORD-{currentYear:D4}{currentMonth:D2}-{(ordersThisMonth + 1):D4}";
                }

                return orderNumber;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured");
                throw;
            }
        }

        // User-specific Operations

        public async Task<IEnumerable<OrderListDto>> GetOrdersByUserIdAsync(int userId)
        {
            try
            {
                var orders = await _unitOfWork.Orders.FindAsync(o => o.UserId == userId);
                return _mapper.Map<IEnumerable<OrderListDto>>(orders.OrderByDescending(o => o.OrderDate));
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error getting orders for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<OrderListDto>> GetOrdersByUserIdAsync(int userId, int takeCount)
        {
            try
            {
                var orders = await _unitOfWork.Orders.FindAsync(o => o.UserId == userId);
                var recentOrders = orders.Take(takeCount);

                return _mapper.Map<IEnumerable<OrderListDto>>(recentOrders.OrderByDescending(o => o.OrderDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for user {UserId}", userId);
                throw;
            }
        }
        public async Task<(IEnumerable<OrderListDto> Orders, int TotalCount)> GetUserOrdersPagedAsync(int userId, int pageNumber, int pageSize)
        {
            try
            {
                var (orders, totalCount) = await _unitOfWork.Orders.GetPagedWithCountAsync(
                        pageNumber,
                        pageSize,
                        o => o.OrderDate,
                        false,
                        o => o.UserId == userId
                    );

                return (_mapper.Map<IEnumerable<OrderListDto>>(orders), totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged orders for user {UserId}", userId);
                throw;
            }
        }

        

        // Status Management

        public async Task<bool> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto updateStatusDto)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("No order found by id: {orderId}", orderId);
                    return false;
                }

                order.Status = updateStatusDto.Status;
                order.Notes = updateStatusDto.Notes;

                switch (updateStatusDto.Status)
                {
                    case OrderStatus.Shipped:
                        order.ShippedDate = DateTime.Now;
                        break;
                    case OrderStatus.Delivered:
                        order.DeliveredDate = DateTime.Now;
                        if(!order.ShippedDate.HasValue)
                            order.ShippedDate = DateTime.Now;
                        break;
                        
                }
                
                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Order with id: {orderId} updated.", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured when updating order");
                throw;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId, string? reason = null)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
                if (order == null)
                {
                    return false;
                }
                var updateStatusDto = new UpdateOrderStatusDto
                {
                    Status = OrderStatus.Cancelled,
                    Notes = reason ?? "Order cancelled by system"
                };

                await _unitOfWork.BeginTransactionAsync();

                var result = await UpdateOrderStatusAsync(orderId, updateStatusDto);
                if (result)
                {
                    await RestoreStockFromCancelledOrderAsync(orderId);
                }

                await _unitOfWork.CommitTransactionAsync();

                return result;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex,"An error occured.");
                throw;
            }
        }

        public async Task<bool> MarkAsShippedAsync(int orderId, string? notes = null)
        {
            var updateStatusDto = new UpdateOrderStatusDto
            {
                Status = OrderStatus.Shipped,
                Notes = notes ?? "Order shipped"
            };

            return await UpdateOrderStatusAsync(orderId, updateStatusDto);
        }

        public async Task<bool> MarkAsDeliveredAsync(int orderId, string? notes = null)
        {
            var updateStatusDto = new UpdateOrderStatusDto
            {
                Status = OrderStatus.Delivered,
                Notes = notes ?? "Order delivered"
            };

            return await UpdateOrderStatusAsync(orderId, updateStatusDto);
        }

        public async Task<IEnumerable<OrderListDto>> GetOrdersByStatusAsync(OrderStatus status)
        {
            try
            {
                var orders = await _unitOfWork.Orders.FindAsync(o => o.Status == status);
                return _mapper.Map<IEnumerable<OrderListDto>>(orders.OrderByDescending(o => o.OrderDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders by status {Status}", status);
                throw;
            }
        }

        // Order Validation & Business Rules

        public async Task<bool> CanOrderBeCancelledAsync(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
                if (order == null) return false;

                return order.Status == OrderStatus.Pending || order.Status == OrderStatus.Processing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if order can be cancelled {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> CanOrderBeModifiedAsync(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
                if(order == null) return false;

                return order.Status == OrderStatus.Pending;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if order can be modified {OrderId}", orderId);
                throw;
            }
        }


        public async Task<bool> ValidateOrderAsync(CreateOrderDto createOrderDto)
        {
            try
            {
                // Basic validation
                if (createOrderDto.UserId <= 0) return false;
                if (!createOrderDto.OrderItems.Any()) return false;
                if (string.IsNullOrWhiteSpace(createOrderDto.ShippingAddress)) return false;

                // User exists validation
                if (!await _unitOfWork.Users.ExistsAsync(createOrderDto.UserId)) return false;

                // Order items validation
                foreach (var item in createOrderDto.OrderItems)
                {
                    if (item.ProductId <= 0 || item.Quantity <= 0 || item.UnitPrice <= 0)
                        return false;

                    // Product exists validation
                    if (!await _unitOfWork.Products.ExistsAsync(item.ProductId))
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating order");
                throw;
            }
        }

        public async Task<bool> ValidateStockAvailabilityAsync(CreateOrderDto createOrderDto)
        {
            try
            {
                foreach (var item in createOrderDto.OrderItems)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product == null || product.StockQuantity < item.Quantity)
                    {
                        _logger.LogWarning("Insufficient stock for product {ProductId}. Required: {Required}, Available: {Available}",
                            item.ProductId, item.Quantity, product?.StockQuantity ?? 0);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating stock availability");
                throw;
            }
        }

        // Search & Filtering

        public async Task<IEnumerable<OrderListDto>> SearchOrdersAsync(string searchTerm)
        {
            try
            {
                var orders = await _unitOfWork.Orders.FindAsync(o =>
                    o.OrderNumber.Contains(searchTerm) ||
                    o.User.FirstName.Contains(searchTerm) ||
                    o.User.LastName.Contains(searchTerm) ||
                    o.User.Email.Contains(searchTerm),
                    o => o.User
                    );

                return _mapper.Map<IEnumerable<OrderListDto>>(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching orders with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<(IEnumerable<OrderListDto> Orders, int TotalCount)> GetOrdersPagedAsync(int pageNumber, int pageSize)
        {
            try
            {
                var (orders, totalCount) = await _unitOfWork.Orders.GetPagedWithCountAsync(pageNumber, pageSize, o => o.OrderDate, false);
                return (_mapper.Map<IEnumerable<OrderListDto>>(orders), totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured");
                throw;
            }
        }

        // Date-based Queries

        public async Task<IEnumerable<OrderListDto>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var orders = await _unitOfWork.Orders.FindAsync(o => endDate >  o.OrderDate && startDate < o.OrderDate, o => o.User);
                return _mapper.Map<IEnumerable<OrderListDto>>(orders);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<IEnumerable<OrderListDto>> GetOrdersCreatedTodayAsync()
        {
            var start = DateTime.Today;
            var end = start.AddDays(1);    

            return await GetOrdersByDateRangeAsync(start, end);
        }

        public async Task<IEnumerable<OrderListDto>> GetOrdersCreatedThisWeekAsync()
        {
            var start = DateTime.Today;
            var end = start.AddDays(7);

            return await GetOrdersByDateRangeAsync(start, end);
        }

        public async Task<IEnumerable<OrderListDto>> GetOrdersCreatedThisMonthAsync()
        {
            var start = DateTime.Today;
            var end = start.AddMonths(1);

            return await GetOrdersByDateRangeAsync(start, end);
        }

        public async Task<IEnumerable<OrderListDto>> GetRecentOrdersAsync(int takeCount = 10)
        {
            var orders = await _unitOfWork.Orders.GetOrderedAsync(o => o.OrderDate, false);
            var recentOrders = orders.Take(takeCount);
            return _mapper.Map<IEnumerable<OrderListDto>>(recentOrders);
        }

        // Order Statistics & Analytics

        public async Task<OrderSummaryDto> GetOrderSummaryAsync()
        {
            try
            {
                var totalOrders = await _unitOfWork.Orders.CountAsync();
                var pendingOrders = await _unitOfWork.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
                var processingOrders = await _unitOfWork.Orders.CountAsync(o => o.Status == OrderStatus.Processing);
                var shippedOrders = await _unitOfWork.Orders.CountAsync(o => o.Status == OrderStatus.Shipped);
                var deliveredOrders = await _unitOfWork.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);
                var cancelledOrders = await _unitOfWork.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled);

                var allOrders = await _unitOfWork.Orders.GetAllAsync();
                var totalRevenue = allOrders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);
                var averageOrderValue = allOrders.Any() ? allOrders.Average(o => o.TotalAmount) : 0;

                return new OrderSummaryDto
                {
                    TotalOrders = totalOrders,
                    PendingOrders = pendingOrders,
                    ProcessingOrders = processingOrders,
                    ShippedOrders = shippedOrders,
                    DeliveredOrders = deliveredOrders,
                    CancelledOrders = cancelledOrders,
                    TotalRevenue = totalRevenue,
                    AverageOrderValue = averageOrderValue
                };

                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order summary");
                throw;
            }
        }

        public async Task<OrderSummaryDto> GetOrderSummaryByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var orders = await _unitOfWork.Orders.FindAsync(o =>
                    o.OrderDate >= startDate && o.OrderDate <= endDate);

                var totalOrders = orders.Count();
                var totalRevenue = orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);
                var averageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0;

                var ordersByStatus = orders.GroupBy(o => o.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new OrderSummaryDto
                {
                    TotalOrders = totalOrders,
                    PendingOrders = ordersByStatus.GetValueOrDefault(OrderStatus.Pending, 0),
                    ProcessingOrders = ordersByStatus.GetValueOrDefault(OrderStatus.Processing, 0),
                    ShippedOrders = ordersByStatus.GetValueOrDefault(OrderStatus.Shipped, 0),
                    DeliveredOrders = ordersByStatus.GetValueOrDefault(OrderStatus.Delivered, 0),
                    CancelledOrders = ordersByStatus.GetValueOrDefault(OrderStatus.Cancelled, 0),
                    TotalRevenue = totalRevenue,
                    AverageOrderValue = averageOrderValue,
                    OrdersByStatus = ordersByStatus
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order summary by date range");
                throw;
            }
        }

        public async Task<int> GetTotalOrdersCountAsync()
        {
            return await _unitOfWork.Orders.CountAsync();
        }

        public async Task<int> GetOrdersCountByStatusAsync(OrderStatus status)
        {
            return await _unitOfWork.Orders.CountAsync(o  => o.Status == status); 
        }

        public async Task<Dictionary<OrderStatus, int>> GetOrdersCountByStatusesAsync()
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            return orders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count());
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            return orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);
        }

        public async Task<decimal> GetRevenueByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _unitOfWork.Orders.FindAsync(o => startDate < o.OrderDate && endDate > o.OrderDate);
            return orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);
        }

        public async Task<decimal> GetAverageOrderValueAsync()
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            return orders.Any() ? orders.Average(o => o.TotalAmount) : 0;
        }

        public async Task<IEnumerable<OrderListDto>> GetTopOrdersByValueAsync(int takeCount = 10)
        {
            var orders = await _unitOfWork.Orders.GetOrderedAsync(o => o.TotalAmount, false);
            var topOrders = orders.Take(takeCount);

            return _mapper.Map<IEnumerable<OrderListDto>>(topOrders);

        }

        public async Task<Dictionary<int, decimal>> GetTopCustomersByOrderValueAsync(int takeCount = 10)
        {
            try
            {
                var orders = await _unitOfWork.Orders.FindAsync(o => o.Status != OrderStatus.Cancelled);
                return orders
                    .GroupBy(o => o.UserId)
                    .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount))
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(takeCount)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top customers by order value");
                throw;
            }
        }

        public async Task<Dictionary<DateTime, decimal>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var orders = await _unitOfWork.Orders
                       .FindAsync(o =>
                           o.OrderDate.Date >= startDate.Date &&
                           o.OrderDate.Date <= endDate.Date &&
                           o.Status != OrderStatus.Cancelled);
                return orders
                    .GroupBy(o => o.OrderDate.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily revenue");
                throw;
            }
        }

        public async Task<Dictionary<DateTime, int>> GetDailyOrderCountAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var orders = await _unitOfWork.Orders
                       .FindAsync(o =>
                           o.OrderDate.Date >= startDate.Date &&
                           o.OrderDate.Date <= endDate.Date &&
                           o.Status != OrderStatus.Cancelled);

                return orders
                    .GroupBy(o => o.OrderDate.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily order count");
                throw;
            }
        }

        // Order Processing Workflow

        public async Task<bool> ProcessOrderAsync(int orderId)
        {
            try
            {
                var updateStatusDto = new UpdateOrderStatusDto
                {
                    Status = OrderStatus.Processing,
                    Notes = "Order is being proccessed"
                };

                return await UpdateOrderStatusAsync(orderId, updateStatusDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured");
                throw;
            }
        }

        public async Task<bool> ConfirmOrderAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Order {OrderId} confirmed", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured");
                throw;
            }
        }



        // Inventory Management

        public async Task<bool> ReserveStockForOrderAsync(CreateOrderDto createOrderDto)
        {
            try
            {
                foreach (var item in createOrderDto.OrderItems)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                        product.UpdatedAt = DateTime.Now;
                        await _unitOfWork.Products.UpdateAsync(product);
                    }

                }
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Stock reserved for order items");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured");
                throw;
            }
        }

        public async Task<bool> ReleaseStockReservationAsync(int orderId)
        {
            try
            {
                var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == orderId);

                foreach (var item in orderItems)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;
                        await _unitOfWork.Products.UpdateAsync(product);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Stock reservation released for order {OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing stock reservation for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> RestoreStockFromCancelledOrderAsync(int orderId)
        {
            return await ReleaseStockReservationAsync(orderId);

        }

        // Other Calculations

        public async Task<decimal> CalculateOrderTotalAsync(CreateOrderDto createOrderDto)
        {
            try
            {
                var subtotal = await CalculateOrderSubtotalAsync(createOrderDto);
                var tax = await CalculateOrderTaxAsync(createOrderDto);
                var discount = await CalculateOrderDiscountAsync(createOrderDto);
                var shipping = await CalculateShippingCostAsync(createOrderDto);

                return subtotal + tax - discount + shipping;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating order total");
                throw;
            }
        }

        public async Task<decimal> CalculateOrderSubtotalAsync(CreateOrderDto createOrderDto)
        {
            await Task.CompletedTask; 
            return createOrderDto.OrderItems.Sum(item => item.UnitPrice * item.Quantity);
        }

        public async Task<decimal> CalculateOrderTaxAsync(CreateOrderDto createOrderDto)
        {
            await Task.CompletedTask; 
            var subtotal = await CalculateOrderSubtotalAsync(createOrderDto);
      
            return subtotal * 0.10m;
        }

        public async Task<decimal> CalculateOrderDiscountAsync(CreateOrderDto createOrderDto)
        {
            await Task.CompletedTask; 
            return createOrderDto.DiscountAmount ?? 0;
        }

        public async Task<decimal> CalculateShippingCostAsync(CreateOrderDto createOrderDto)
        {
            await Task.CompletedTask; // Placeholder for async pattern
            var subtotal = await CalculateOrderSubtotalAsync(createOrderDto);

            // Free shipping for orders over $100
            if (subtotal >= 100m)
                return 0m;

            // Standard shipping cost
            return 15.00m;
        }

        // Order Items Management
        public async Task<bool> AddOrderItemAsync(int orderId, CreateOrderItemDto orderItemDto)
        {
            try
            {
                if (!await CanOrderBeModifiedAsync(orderId))
                {
                    _logger.LogWarning("Cannot add item to order {OrderId} - order cannot be modified", orderId);
                    return false;
                }

                var orderItem = _mapper.Map<OrderItem>(orderItemDto);
                orderItem.OrderId = orderId;
                orderItem.TotalPrice = orderItemDto.UnitPrice * orderItemDto.Quantity - (orderItemDto.DiscountAmount ?? 0);

                await _unitOfWork.OrderItems.AddAsync(orderItem);
                await _unitOfWork.SaveChangesAsync();

                // Recalculate order total
                await RecalculateOrderTotalAsync(orderId);

                _logger.LogInformation("Order item added to order {OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to order {OrderId}", orderId);
                throw;
            }
        }
        public async Task<bool> UpdateOrderItemAsync(int orderId, int orderItemId, CreateOrderItemDto orderItemDto)
        {
            try
            {
                if (!await CanOrderBeModifiedAsync(orderId))
                {
                    return false;
                }

                var orderItem = await _unitOfWork.OrderItems.FirstOrDefaultAsync(oi =>
                    oi.Id == orderItemId && oi.OrderId == orderId);

                if (orderItem == null)
                {
                    return false;
                }

                _mapper.Map(orderItemDto, orderItem);
                orderItem.TotalPrice = orderItemDto.UnitPrice * orderItemDto.Quantity - (orderItemDto.DiscountAmount ?? 0);

                await _unitOfWork.OrderItems.UpdateAsync(orderItem);
                await _unitOfWork.SaveChangesAsync();

                await RecalculateOrderTotalAsync(orderId);

                _logger.LogInformation("Order item {OrderItemId} updated in order {OrderId}", orderItemId, orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order item {OrderItemId} in order {OrderId}", orderItemId, orderId);
                throw;
            }
        }

        public async Task<bool> RemoveOrderItemAsync(int orderId, int orderItemId)
        {
            try
            {
                if (!await CanOrderBeModifiedAsync(orderId))
                {
                    return false;
                }

                var orderItem = await _unitOfWork.OrderItems.FirstOrDefaultAsync(oi =>
                    oi.Id == orderItemId && oi.OrderId == orderId);

                if (orderItem == null)
                {
                    return false;
                }

                await _unitOfWork.OrderItems.DeleteAsync(orderItemId);
                await _unitOfWork.SaveChangesAsync();

                await RecalculateOrderTotalAsync(orderId);

                _logger.LogInformation("Order item {OrderItemId} deleted in order {OrderId}", orderItemId, orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order item {OrderItemId} in order {OrderId}", orderItemId, orderId);
                throw;
            }
        }

        public async Task<bool> RecalculateOrderTotalAsync(int orderId)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
                var orderItems = await _unitOfWork.OrderItems.FindAsync(oi => oi.OrderId == orderId);

                if (order == null)
                {
                    return false;
                }
                var subtotal = orderItems.Sum(oi => oi.TotalPrice);
                var tax = subtotal * 0.10m; // 10% tax
                var discount = order.DiscountAmount ?? 0;
                var shipping = subtotal >= 100m ? 0m : 15.00m;

                order.TotalAmount = (decimal)(subtotal + tax - discount + shipping);

                order.TaxAmount = tax;

                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating total for order {OrderId}", orderId);

                throw;
            }
        }

        // Bulk Operations

        public async Task<IEnumerable<OrderDto>> GetOrdersByIdsAsync(IEnumerable<int> orderIds)
        {
            try
            {
                var orders = await _unitOfWork.Orders.FindAsync(o => orderIds.Contains(o.Id));
                if (orders == null) return null;

                return _mapper.Map<IEnumerable<OrderDto>>(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured");
                throw;
            }
        }

        public async Task<bool> BulkUpdateOrderStatusAsync(IEnumerable<int> orderIds, OrderStatus newStatus, string? notes = null)
        {
            try
            {
                var orders = await _unitOfWork.Orders.FindAsync(o => orderIds.Contains(o.Id));
                if (orders == null) return false;

                foreach (var order in orders)
                {
                    order.Status = newStatus;
                    order.Notes = notes ?? "Status updated";

        
                    switch (newStatus)
                    {
                        case OrderStatus.Shipped:
                            order.ShippedDate = DateTime.UtcNow;
                            break;
                        case OrderStatus.Delivered:
                            order.DeliveredDate = DateTime.UtcNow;
                            if (!order.ShippedDate.HasValue)
                                order.ShippedDate = DateTime.UtcNow;
                            break;
                    }
                }

                await _unitOfWork.Orders.UpdateRangeAsync(orders);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Orders updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured");
                throw;
            }
        }

        public async Task<bool> BulkCancelOrdersAsync(IEnumerable<int> orderIds, string? reason = null)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                foreach (var orderId in orderIds)
                {
                    if (await CanOrderBeCancelledAsync(orderId))
                    {
                        await CancelOrderAsync(orderId, reason);
                    }
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Bulk cancellation completed for orders");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured");
                throw;
            }
        }
    }
}
