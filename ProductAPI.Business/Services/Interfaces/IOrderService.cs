using ProductAPI.Business.DTOs.Order;
using ProductAPI.Domain.Enums;

namespace ProductAPI.Business.Services.Interfaces
{
    public interface IOrderService
    {
        // Basic CRUD Operations
        Task<OrderDto?> GetOrderByIdAsync(int id);
        Task<OrderDto?> GetOrderWithItemsAsync(int id);
        Task<IEnumerable<OrderListDto>> GetAllOrdersAsync();
        Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto);
        Task<OrderDto?> UpdateOrderAsync(int id, UpdateOrderDto updateOrderDto);
        Task<bool> DeleteOrderAsync(int id);

        // Order Number Operations
        Task<OrderDto?> GetOrderByOrderNumberAsync(string orderNumber);
        Task<string> GenerateOrderNumberAsync();

        // User-specific Operations
        Task<IEnumerable<OrderListDto>> GetOrdersByUserIdAsync(int userId);
        Task<IEnumerable<OrderListDto>> GetOrdersByUserIdAsync(int userId, int takeCount);
        Task<(IEnumerable<OrderListDto> Orders, int TotalCount)> GetUserOrdersPagedAsync(int userId, int pageNumber, int pageSize);

        // Status Management
        Task<bool> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto updateStatusDto);
        Task<bool> CancelOrderAsync(int orderId, string? reason = null);
        Task<bool> MarkAsShippedAsync(int orderId, string? notes = null);
        Task<bool> MarkAsDeliveredAsync(int orderId, string? notes = null);
        Task<IEnumerable<OrderListDto>> GetOrdersByStatusAsync(OrderStatus status);

        // Order Validation & Business Rules
        Task<bool> CanOrderBeCancelledAsync(int orderId);
        Task<bool> CanOrderBeModifiedAsync(int orderId);
        Task<bool> ValidateOrderAsync(CreateOrderDto createOrderDto);
        Task<bool> ValidateStockAvailabilityAsync(CreateOrderDto createOrderDto);

        // Search & Filtering
        Task<IEnumerable<OrderListDto>> SearchOrdersAsync(string searchTerm);
        Task<(IEnumerable<OrderListDto> Orders, int TotalCount)> GetOrdersPagedAsync(int pageNumber, int pageSize);
    

        // Date-based Queries
        Task<IEnumerable<OrderListDto>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<OrderListDto>> GetOrdersCreatedTodayAsync();
        Task<IEnumerable<OrderListDto>> GetOrdersCreatedThisWeekAsync();
        Task<IEnumerable<OrderListDto>> GetOrdersCreatedThisMonthAsync();
        Task<IEnumerable<OrderListDto>> GetRecentOrdersAsync(int takeCount = 10);

        // Order Statistics & Analytics
        Task<OrderSummaryDto> GetOrderSummaryAsync();
        Task<OrderSummaryDto> GetOrderSummaryByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<int> GetTotalOrdersCountAsync();
        Task<int> GetOrdersCountByStatusAsync(OrderStatus status);
        Task<Dictionary<OrderStatus, int>> GetOrdersCountByStatusesAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetRevenueByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetAverageOrderValueAsync();

        // Advanced Analytics
        Task<IEnumerable<OrderListDto>> GetTopOrdersByValueAsync(int takeCount = 10);
        Task<Dictionary<int, decimal>> GetTopCustomersByOrderValueAsync(int takeCount = 10);
        Task<Dictionary<DateTime, decimal>> GetDailyRevenueAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<DateTime, int>> GetDailyOrderCountAsync(DateTime startDate, DateTime endDate);

        // Order Processing Workflow
        Task<bool> ProcessOrderAsync(int orderId);
        Task<bool> ConfirmOrderAsync(int orderId);

        // Inventory Management
        Task<bool> ReserveStockForOrderAsync(CreateOrderDto createOrderDto);
        Task<bool> ReleaseStockReservationAsync(int orderId);
        Task<bool> RestoreStockFromCancelledOrderAsync(int orderId);

        // Order Calculations
        Task<decimal> CalculateOrderTotalAsync(CreateOrderDto createOrderDto);
        Task<decimal> CalculateOrderSubtotalAsync(CreateOrderDto createOrderDto);
        Task<decimal> CalculateOrderTaxAsync(CreateOrderDto createOrderDto);
        Task<decimal> CalculateOrderDiscountAsync(CreateOrderDto createOrderDto);
        Task<decimal> CalculateShippingCostAsync(CreateOrderDto createOrderDto);

        // Order Items Management
        Task<bool> AddOrderItemAsync(int orderId, CreateOrderItemDto orderItemDto);
        Task<bool> UpdateOrderItemAsync(int orderId, int orderItemId, CreateOrderItemDto orderItemDto);
        Task<bool> RemoveOrderItemAsync(int orderId, int orderItemId);
        Task<bool> RecalculateOrderTotalAsync(int orderId);

        // Bulk Operations
        Task<IEnumerable<OrderDto>> GetOrdersByIdsAsync(IEnumerable<int> orderIds);
        Task<bool> BulkUpdateOrderStatusAsync(IEnumerable<int> orderIds, OrderStatus newStatus, string? notes = null);
        Task<bool> BulkCancelOrdersAsync(IEnumerable<int> orderIds, string? reason = null);
        
    }
}
