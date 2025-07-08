
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductAPI.Domain.Entities;
using ProductAPI.Domain.Enums;


namespace ProductAPI.DataAccess.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Table name
            builder.ToTable("Users");

            // Primary Key
            builder.HasKey(u => u.UserId);

            // Properties
            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(u => u.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETDATE()");

            builder.Property(u => u.UpdatedAt);

            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            builder.Property(u => u.IsEmailVerified)
                .HasDefaultValue(false);

            // Enum to int conversion
            builder.Property(u => u.Role)
                .HasConversion<int>()
                .HasDefaultValue(UserRole.User);

            // Password fields
            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.PasswordResetRequired)
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            // Relationships
            builder.HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}