using ProductAPI.Business.DTOs.User;
using ProductAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductAPI.Business.Services.Interfaces
{
    public interface IUserService
    {
        //Basic CRUD Operations
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserProfileDto?> GetUserProfileByIdAsync(int id);
        Task<UserDto?> GetUserWithOrdersAsync(int id);
        Task<UserDto?> GetUserWithOrdersAsync(int id, int orderCount);
        Task<IEnumerable<UserListDto>> GetAllUsersAsync();
        Task<UserDto?> CreateUserAsync(CreateUserDto createuserDto);
        Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int id);

        //Email Operations
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<bool> IsEmailAvailableAsync(string email);
        Task<bool> IsEmailAvailableAsync(string email, int excludeUserId);

        // Authentication & Authorization
        Task<bool> ValidateUserCredentialsAsync(string email, string password);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<bool> ResetPasswordAsync(string email);
        Task<bool> VerifyEmailAsync(int userId, string verificationToken);

        // User Status Management
        Task<bool> ActivateUserAsync(int userId);
        Task<bool> DeactivateUserAsync(int userId);
        Task<bool> ToggleUserStatusAsync(int userId);
        Task<IEnumerable<int>> ActivateUsersAsync(IEnumerable<int> userIds);
        Task<IEnumerable<int>> DeactivateUsersAsync(IEnumerable<int> userIds);

        //Role Management
        Task<bool> ChangeUserRoleAsync(int userId, UserRole newRole);
        Task<IEnumerable<UserListDto>> GetUsersByRoleAsync(UserRole role);
        Task<IEnumerable<UserListDto>> GetActiveUsersByRoleAsync(UserRole role);

        //Query Operations
        Task<IEnumerable<UserListDto>> GetActiveUsersAsync();
        Task<IEnumerable<UserListDto>> GetInactiveUsersAsync();
        Task<IEnumerable<UserListDto>> GetUnverifiedUsersAsync();
        Task<IEnumerable<UserListDto>> SearchUsersAsync(string searchTerm);
        Task<IEnumerable<UserListDto>> SearchUsersAsync(string searchTerm, int pageNumber, int pageSize);

        // Pagination
        Task<(IEnumerable<UserListDto> Users, int TotalCount)> GetUsersPagedAsync(int pageNumber, int pageSize);
        Task<(IEnumerable<UserListDto> Users, int TotalCount)> GetUsersPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, UserRole? role = null, bool? isActive = null);

        // Statistics
        Task<UserSummaryDto> GetUserSummaryAsync();
        Task<int> GetTotalUsersCountAsync();
        Task<int> GetActiveUsersCountAsync();
        Task<int> GetUserCountByRoleAsync(UserRole role);
        Task<Dictionary<UserRole, int>> GetUsersCountByRolesAsync();

        // Recent Activities
        Task<IEnumerable<UserListDto>> GetRecentlyRegisteredUsersAsync(int count = 10);
        Task<IEnumerable<UserListDto>> GetUsersRegisteredBetweenAsync(DateTime start, DateTime end);
        Task<IEnumerable<UserListDto>> GetUsersRegisteredTodayAsync();
        Task<IEnumerable<UserListDto>> GetUsersRegisteredThisWeekAsync();
        Task<IEnumerable<UserListDto>> GetUsersRegisteredThisMonthAsync();

        // Bulk Operations
        Task<IEnumerable<UserListDto>> GetUsersByIdsAsync(IEnumerable<int> ids);
        Task<bool> BulkUpdateUsersAsync(IEnumerable<int> ids, bool? isActive = null, UserRole? userRole = null);
        Task<bool> BulkDeleteUsersAsync(IEnumerable<int> ids);

        // Validation & Business Rules
        Task<bool> UserExistsAsync(int userId);
        Task<bool> CanUserBeDeletedAsync(int userId);
        Task<bool> CanUserRoleBeChangedAsync(int userId, UserRole role);

        // Export Operations
        Task<byte[]> ExportUsersToExcelAsync();
        Task<byte[]> ExportUsersToExcelAsync(UserRole? role = null, bool? isActive = null);
        Task<string> ExportUsersToCsvAsync();

    }
}
