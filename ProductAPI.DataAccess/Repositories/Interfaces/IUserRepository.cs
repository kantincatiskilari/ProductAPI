using API.DataAccess.Repositories.Interfaces;
using ProductAPI.Domain.Entities;
using ProductAPI.Domain.Enums;

namespace ProductAPI.DataAccess.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        //Email Based Operations
        Task<User?> GetByEmailAsync(string email);
        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsEmailExistsAsync(string email, int excludeUserId);

        //User-Related Data
        Task<User?> GetUserWithOrdersAsync(int id);
        Task<User?> GetUserWithOrdersAsync(int id, int takeOrdersCount);
        Task<User?> GetUserProfileAsync(int id);

        //Role-based Queries
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole userRole);
        Task<IEnumerable<User>> GetActiveUsersByRoleAsync(UserRole userRole);

        //Status-based Queries
        Task<IEnumerable<User>> GetActiveUsersAsync();
        Task<IEnumerable<User>> GetInactiveUsersAsync();
        Task<IEnumerable<User>> GetUnverifiedUsersAsync();

        //Search Operations
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int pageNumber, int pageSize);

        //Statistics
        Task<int> GetActiveUsersCountAsync();
        Task<int> GetUserCountByRoleAsync(UserRole userRole);
        Task<Dictionary<UserRole, int>> GetUsersCountByRolesAsync();

        //Bulk Operations
        Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<int> ids);
        Task ActivateUsersAsync(IEnumerable<int> userIds);
        Task DeactivateUsersAsync(IEnumerable<int> userIds);

        //Recent Activities
        Task<IEnumerable<User>> GetRecentlyRegisteredUsersAsync(int takeCount = 10);
        Task<IEnumerable<User>> GetUsersRegisteredBetweenAsync(DateTime startDate, DateTime endDate);
    }
}
