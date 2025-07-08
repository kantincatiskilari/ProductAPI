using ProductAPI.Domain.Enums;

namespace ProductAPI.Domain.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }
        public UserRole Role { get; set; } = UserRole.User;
        public string PasswordHash { get; set; }
        public bool PasswordResetRequired { get; set; } = false;

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    }
}
