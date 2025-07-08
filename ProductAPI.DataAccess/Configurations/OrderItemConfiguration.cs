using ProductAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductAPI.Domain.Enums;

namespace ProductAPI.DataAccess.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            //Table Name
            builder.ToTable("OrderItems");

            //P.K
            builder.HasKey(oi => oi.Id);

            //Properties
            builder.Property(oi => oi.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(oi => oi.ProductId)
                .IsRequired();

            builder.Property(oi => oi.OrderId)
                .IsRequired();


            builder.Property(oi => oi.ProductDescription)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(oi => oi.UnitPrice)
                .IsRequired()
                 .HasColumnType("decimal(18,2)")
                 .HasPrecision(18, 2);

            builder.Property(oi => oi.Quantity)
                .IsRequired();

            builder.Property(oi => oi.DiscountAmount)
                .IsRequired()
                 .HasColumnType("decimal(18,2)")
                 .HasPrecision(18, 2);

            builder.Property(oi => oi.TotalPrice)
               .IsRequired()
               .HasColumnType("decimal(18,2)")
               .HasPrecision(18, 2);

            // Indexes
            builder.HasIndex(oi => oi.OrderId)
                .HasDatabaseName("IX_OrderItems_OrderId");

            builder.HasIndex(oi => oi.ProductId)
                .HasDatabaseName("IX_OrderItems_ProductId");

            // Composite index for unique constraint
            builder.HasIndex(oi => new { oi.OrderId, oi.ProductId })
                .IsUnique()
                .HasDatabaseName("IX_OrderItems_OrderId_ProductId");


            //Relationships

            builder.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(o => o.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

           builder.HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Check constraints
            builder.HasCheckConstraint("CK_OrderItems_UnitPrice", "[UnitPrice] >= 0");
            builder.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0");
            builder.HasCheckConstraint("CK_OrderItems_TotalPrice", "[TotalPrice] >= 0");
            builder.HasCheckConstraint("CK_OrderItems_DiscountAmount", "[DiscountAmount] >= 0");
        }

        
    }
}
