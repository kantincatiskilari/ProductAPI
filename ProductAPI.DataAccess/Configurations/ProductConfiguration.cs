using ProductAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductAPI.Domain.Enums;

namespace ProductAPI.DataAccess.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            //Table Name
            builder.ToTable("Products");

            //Primary Key
            builder.HasKey(p => p.Id);

            //Properties
            builder.Property(p =>  p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .HasMaxLength(1000);

            builder.Property(p => p.SKU)
                .HasMaxLength(50);

            builder.Property(p => p.Price)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasPrecision(18, 2);

            builder.Property(p => p.StockQuantity)
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(p => p.IsActive)
                .HasDefaultValue(true);

            builder.Property(p => p.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            builder.Property(p => p.UpdatedAt);

            // Indexes
            builder.HasIndex(p => p.SKU)
                .IsUnique()
                .HasDatabaseName("IX_Products_SKU")
                .HasFilter("[SKU] IS NOT NULL"); 

            builder.HasIndex(p => p.Name)
                .HasDatabaseName("IX_Products_Name");

            builder.HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_Products_IsActive");

            //Relationships
            builder.HasMany(p => p.OrderItems)
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
