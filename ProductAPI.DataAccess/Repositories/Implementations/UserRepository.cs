using Microsoft.EntityFrameworkCore;
using ProductAPI.DataAccess.Context;
using ProductAPI.DataAccess.Repositories.Interfaces;
using ProductAPI.Domain.Entities;
using ProductAPI.Domain.Enums;
using System.Linq.Expressions;

namespace ProductAPI.DataAccess.Repositories.Implementations
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }

        //Email Based Operations
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }
        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> IsEmailExistsAsync(string email, int excludeUserId)
        {
            return await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.UserId != excludeUserId);
        }

        // User with related data
        public async Task<User?> GetUserWithOrdersAsync(int id)
        {
            return await _dbSet
                .Include(u => u.Orders)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetUserWithOrdersAsync(int id, int takeOrdersCount)
        {
            return await _dbSet
                .Include(u => u.Orders.OrderByDescending(o => o.OrderDate).Take(takeOrdersCount))
                .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetUserProfileAsync(int id)
        {
            return await _dbSet
                .Select(u => new User
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    IsEmailVerified = u.IsEmailVerified,
                    CreatedAt = u.CreatedAt
                })
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        //Role-based Queries

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole userRole)
        {
            return await _dbSet.Where(u => u.Role == userRole).ToListAsync();
        }
        public async Task<IEnumerable<User>> GetActiveUsersByRoleAsync(UserRole userRole)
        {
            return await _dbSet.Where(u => u.IsActive && u.Role == userRole).ToListAsync();
        }



        //Status-based Queries
        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            return await _dbSet.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
        }
        public async Task<IEnumerable<User>> GetInactiveUsersAsync()
        {
            return await _dbSet.Where(u => u.IsActive == false).OrderBy(u => u.FirstName).ToListAsync();
        }
        public async Task<IEnumerable<User>> GetUnverifiedUsersAsync()
        {
            return await _dbSet.Where(u => u.IsEmailVerified == false).OrderBy(u => u.CreatedAt).ToListAsync();
        }

        //Search Operations
        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            var term = searchTerm.ToLower();
            return await _dbSet
                .Where(u => u.FirstName.ToLower().Contains(term) ||
                           u.LastName.ToLower().Contains(term) ||
                           u.Email.ToLower().Contains(term))
                .OrderBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var term = searchTerm.ToLower();
            return await _dbSet
                .Where(u => u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term))
                .OrderBy(u => u.FirstName)
                .Skip((pageNumber - 1) *  pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        //Statistics
        public async Task<int> GetActiveUsersCountAsync()
        {
            return await _dbSet.CountAsync(u => u.IsActive);
        }
        public async Task<int> GetUserCountByRoleAsync(UserRole userRole)
        {
            return await _dbSet.CountAsync(u => u.IsActive && u.Role == userRole);
        }
        public async Task<Dictionary<UserRole, int>> GetUsersCountByRolesAsync()
        {
            return await _dbSet
                    .GroupBy(u => u.Role)
                    .Select(g => new { UserRole = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserRole, x => x.Count);
        }

        //Bulk Operations
        public async Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<int> ids)
        {
            return await _dbSet.Where(u => ids.Contains(u.UserId)).ToListAsync();
        }
        public async Task ActivateUsersAsync(IEnumerable<int> userIds)
        {
            var users = await _dbSet.Where(u => userIds.Contains(u.UserId)).ToListAsync();
            foreach (var user in users)
            {
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
            }  
        }

        public  async Task DeactivateUsersAsync(IEnumerable<int> userIds)
        {
            var users = await _dbSet.Where(u => userIds.Contains(u.UserId)).ToListAsync();
            foreach (var user in users)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        //Recent Activities
        public async Task<IEnumerable<User>> GetUsersRegisteredBetweenAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                    .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                    .OrderBy(u => u.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetRecentlyRegisteredUsersAsync(int takeCount = 10)
        {
            return await _dbSet.OrderByDescending(u => u.CreatedAt).Take(takeCount).ToListAsync();
        }

    }
}
