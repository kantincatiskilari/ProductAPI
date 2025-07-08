using ProductAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductAPI.Domain.Enums;

namespace ProductAPI.DataAccess.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            //Table Name
            builder.ToTable("Orders");

            //P.K
            builder.HasKey(o => o.Id);

            //Properties
            builder.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(o => o.TotalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasPrecision(18, 2);

            builder.Property(o => o.DiscountAmount)
                 .HasColumnType("decimal(18,2)")
                 .HasPrecision(18, 2);

            builder.Property(o => o.TaxAmount)
                 .HasColumnType("decimal(18,2)")
                 .HasPrecision(18, 2);

            builder.Property(o => o.OrderDate)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            builder.Property(o => o.ShippedDate);

            builder.Property(o => o.DeliveredDate);

            //ENUM -> INT
            builder.Property(o => o.Status)
                .HasConversion<int>()
                .HasDefaultValue(OrderStatus.Pending);

            builder.Property(o => o.Notes)
                .HasMaxLength(500);

            builder.Property(o => o.ShippingAddress)
                .HasMaxLength(1000);

            // Indexes
            builder.HasIndex(o => o.OrderNumber)
                .IsUnique()
                .HasDatabaseName("IX_Orders_OrderNumber");

            builder.HasIndex(o => o.UserId)
                .HasDatabaseName("IX_Orders_UserId");

            builder.HasIndex(o => o.Status)
                .HasDatabaseName("IX_Orders_Status");

            builder.HasIndex(o => o.OrderDate)
                .HasDatabaseName("IX_Orders_OrderDate");

            // Composite index for common queries
            builder.HasIndex(o => new { o.UserId, o.Status })
                .HasDatabaseName("IX_Orders_UserId_Status");

            builder.HasIndex(o => new { o.OrderDate, o.Status })
                .HasDatabaseName("IX_Orders_OrderDate_Status");

            //Relationships

            builder.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Check constraints (optional - business rules)
            builder.HasCheckConstraint("CK_Orders_TotalAmount", "[TotalAmount] >= 0");
            builder.HasCheckConstraint("CK_Orders_DiscountAmount", "[DiscountAmount] >= 0");
            builder.HasCheckConstraint("CK_Orders_TaxAmount", "[TaxAmount] >= 0");
        }
    }
}
