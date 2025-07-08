using AutoMapper;
using ProductAPI.Business.DTOs.Order;
using ProductAPI.Business.DTOs.Product;
using ProductAPI.Business.DTOs.User;
using ProductAPI.Domain.Entities;
using ProductAPI.Domain.Enums;

namespace ProductAPI.Business.Mappings
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles() 
        {
            ConfigureUserMappings();
            ConfigureProductMappings();
            ConfigureOrderMappings();
            ConfigureOrderItemMappings();
        }

        private void ConfigureUserMappings()
        {
            //Entity to DTOs
            CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.RoleDisplayName, opt => opt.MapFrom(src => src.Role.ToString()));

            CreateMap<User, UserProfileDto>()
             .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

            CreateMap<User, UserListDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.RoleDisplayName, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"));

            //DTOs to Entity
            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.Orders, opt => opt.Ignore());

            CreateMap<UpdateUserDto, User>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsEmailVerified, opt => opt.Ignore())
                .ForMember(dest => dest.Orders, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        }

        private void ConfigureProductMappings()
        {
            //Entity to DTOs
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.IsInStock, opt => opt.MapFrom(src => src.StockQuantity > 0))
                .ForMember(dest => dest.IsLowStock, opt => opt.MapFrom(src => src.StockQuantity > 0 && src.StockQuantity <= 10))
                .ForMember(dest => dest.IsOutOfStock, opt => opt.MapFrom(src => src.StockQuantity == 0))
                .ForMember(dest => dest.StockStatus, opt => opt.MapFrom(src => GetStockStatus(src.StockQuantity)))
                .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom(src => $"${src.Price:F2}"));

            CreateMap<Product, ProductListDto>()
                .ForMember(dest => dest.IsInStock, opt => opt.MapFrom(src => src.StockQuantity > 0))
                .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"))
                .ForMember(dest => dest.StockStatus, opt => opt.MapFrom(src => GetStockStatus(src.StockQuantity)))
                .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom(src => $"${src.Price:F2}"));

            //DTOs to Entity
            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.OrderItems, opt => opt.Ignore());

            CreateMap<UpdateProductDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.OrderItems, opt => opt.Ignore());

            CreateMap<UpdateStockDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Name, opt => opt.Ignore())
                .ForMember(dest => dest.Description, opt => opt.Ignore())
                .ForMember(dest => dest.SKU, opt => opt.Ignore())
                .ForMember(dest => dest.Price, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.OrderItems, opt => opt.Ignore());

        }

        private void ConfigureOrderMappings()
        {
            //Entity to DTOs
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.FinalAmount, opt => opt.MapFrom(src => src.TotalAmount - (src.DiscountAmount ?? 0)))
                .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.ItemsCount, opt => opt.MapFrom(src => src.OrderItems.Count))
                .ForMember(dest => dest.IsShipped, opt => opt.MapFrom(src => src.ShippedDate.HasValue))
                .ForMember(dest => dest.IsDelivered, opt => opt.MapFrom(src => src.DeliveredDate.HasValue))
                .ForMember(dest => dest.CanBeCancelled, opt => opt.MapFrom(src =>
                    src.Status == OrderStatus.Pending || src.Status == OrderStatus.Processing));

            CreateMap<Order, OrderListDto>()
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"))
                .ForMember(dest => dest.FinalAmount, opt => opt.MapFrom(src => src.TotalAmount - (src.DiscountAmount ?? 0)))
                .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.OrderDateDisplay, opt => opt.MapFrom(src => src.OrderDate.ToString("dd/MM/yyyy HH:mm")));

            //DTOs to Entity
            CreateMap<CreateOrderDto, Order>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OrderNumber, opt => opt.Ignore())
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => OrderStatus.Pending))
                .ForMember(dest => dest.ShippedDate, opt => opt.Ignore())
                .ForMember(dest => dest.DeliveredDate, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.OrderItems, opt => opt.Ignore());

            CreateMap<UpdateOrderDto, Order>()
               .ForMember(dest => dest.Id, opt => opt.Ignore())
               .ForMember(dest => dest.OrderNumber, opt => opt.Ignore())
               .ForMember(dest => dest.UserId, opt => opt.Ignore())
               .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
               .ForMember(dest => dest.DiscountAmount, opt => opt.Ignore())
               .ForMember(dest => dest.TaxAmount, opt => opt.Ignore())
               .ForMember(dest => dest.OrderDate, opt => opt.Ignore())
               .ForMember(dest => dest.User, opt => opt.Ignore())
               .ForMember(dest => dest.OrderItems, opt => opt.Ignore())
               .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateOrderStatusDto, Order>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OrderNumber, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.DiscountAmount, opt => opt.Ignore())
                .ForMember(dest => dest.TaxAmount, opt => opt.Ignore())
                .ForMember(dest => dest.OrderDate, opt => opt.Ignore())
                .ForMember(dest => dest.ShippingAddress, opt => opt.Ignore())
                .ForMember(dest => dest.ShippedDate, opt => opt.MapFrom(src => src.Status == OrderStatus.Shipped ? DateTime.UtcNow : (DateTime?)null))
                .ForMember(dest => dest.DeliveredDate, opt => opt.MapFrom(src => src.Status == OrderStatus.Delivered ? DateTime.UtcNow : (DateTime?)null))
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.OrderItems, opt => opt.Ignore());
        }

        private void ConfigureOrderItemMappings()
        {
            // Entity to DTOs
            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(dest => dest.FinalPrice, opt => opt.MapFrom(src => src.TotalPrice - (src.DiscountAmount ?? 0)))
                .ForMember(dest => dest.UnitPriceAfterDiscount, opt => opt.MapFrom(src =>
                    (src.TotalPrice - (src.DiscountAmount ?? 0)) / src.Quantity));

            // DTOs to Entity
            CreateMap<CreateOrderItemDto, OrderItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OrderId, opt => opt.Ignore()) // Set by service
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => (src.UnitPrice * src.Quantity) - (src.DiscountAmount ?? 0)))
                .ForMember(dest => dest.Order, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore());
        }

        // Helper methods
        private static string GetStockStatus(int stockQuantity)
        {
            return stockQuantity switch
            {
                0 => "Out of Stock",
                <= 10 => "Low Stock",
                _ => "In Stock"
            };
        }
    }
}
