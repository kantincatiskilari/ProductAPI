
using AutoMapper;
using Microsoft.Extensions.Logging;
using ProductAPI.Business.DTOs.User;
using ProductAPI.Business.Services.Interfaces;
using ProductAPI.DataAccess.UnitOfWork;
using ProductAPI.Domain.Entities;
using ProductAPI.Domain.Enums;
using System.Linq.Expressions;

namespace ProductAPI.Business.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        //Helpers
        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Basic CRUD Operations
        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);

                if (user == null)
                {
                    _logger.LogWarning($"User with ID: {id} not found", id);
                    return null;
                }

                return _mapper.Map<UserDto>(user);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by ID: {id}", id);
                throw;
            }
        }

        public async Task<UserProfileDto?> GetUserProfileByIdAsync(int id)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);

                if (user == null)
                {
                    _logger.LogWarning($"User with ID: {id} not found", id);
                    return null;
                }

                return _mapper.Map<UserProfileDto>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by ID: {id}", id);
                throw;
            }
        }

        public async Task<UserDto?> GetUserWithOrdersAsync(int id, int orderCount)
        {
            try
            {
                var userWithOrders = await _unitOfWork.Users.GetUserWithOrdersAsync(id, orderCount);

                if (userWithOrders == null)
                {
                    _logger.LogWarning($"User with ID: {id} not found", id);
                    return null;
                }

                return _mapper.Map<UserDto>(userWithOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by ID: {id}", id);
                throw;
            }
        }
        public async Task<UserDto?> GetUserWithOrdersAsync(int id)
        {
            try
            {
                var userWithOrders = await _unitOfWork.Users.GetUserWithOrdersAsync(id);

                if (userWithOrders == null)
                {
                    _logger.LogWarning($"User with ID: {id} not found", id);
                    return null;
                }

                return _mapper.Map<UserDto>(userWithOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by ID: {id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<UserListDto>> GetAllUsersAsync()
        {
            try
            {
                var users = await _unitOfWork.Users.GetAllAsync();

                return _mapper.Map<IEnumerable<UserListDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }
        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                // Email uniqueness validation
                if (await _unitOfWork.Users.IsEmailExistsAsync(createUserDto.Email))
                {
                    throw new InvalidOperationException($"Email {createUserDto.Email} is already in use");
                }

                var user = _mapper.Map<User>(createUserDto);

                // Generate temporary password for new users
                string temporaryPassword = GenerateTemporaryPassword();
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword, 12);

                user.CreatedAt = DateTime.UtcNow;
                user.IsActive = true;
                user.IsEmailVerified = false;
                user.PasswordResetRequired = true; // Force password change on first login

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // TODO: Send welcome email with temporary password
                // await _emailService.SendWelcomeEmailAsync(user.Email, temporaryPassword);

                _logger.LogInformation("User created successfully with ID {UserId}", user.UserId);
                return _mapper.Map<UserDto>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with email {Email}", createUserDto.Email);
                throw;
            }
        }

        public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);

                if (user == null)
                {
                    _logger.LogWarning($"User with ID: {id} not found", id);
                    return null;
                }

                // Email uniqueness validation (excluding current user)
                if (await _unitOfWork.Users.IsEmailExistsAsync(updateUserDto.Email, id))
                {
                    throw new InvalidOperationException($"Email {updateUserDto.Email} is already in use by another user");
                }

                _mapper.Map(updateUserDto, user);
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("User with ID {UserId} updated successfully", id);
                return _mapper.Map<UserDto>(user);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                if (!await CanUserBeDeletedAsync(id))
                {
                    _logger.LogWarning("User with ID {UserId} cannot be deleted due to business rules", id);
                    return false;
                }

                var user = await _unitOfWork.Users.GetByIdAsync(id);

                if (user == null)
                {
                    _logger.LogWarning($"User with ID: {id} not found", id);
                    return false;
                }

                await _unitOfWork.Users.DeleteAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("User with ID {UserId} deleted successfully", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
                throw;
            }
        }

        // Email Operations
        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning($"User with email: {email} not found", email);
                    return null;
                }

                return _mapper.Map<UserDto?>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user with email: {email}", email);
                throw;
            }
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            try
            {
                bool isEmailExist = await _unitOfWork.Users.IsEmailExistsAsync(email);

                if (isEmailExist)
                {
                    _logger.LogWarning($"Email: {email} in use", email);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when checking email availability", email);
                throw;
            }

        }

        public async Task<bool> IsEmailAvailableAsync(string email, int excludeUserId)
        {
            try
            {
                bool isEmailExist = await _unitOfWork.Users.IsEmailExistsAsync(email, excludeUserId);

                if (isEmailExist)
                {
                    _logger.LogWarning($"Email: {email} in use", email);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email availability {Email} excluding user {UserId}", email, excludeUserId);
                throw;
            }
        }

        // Authentication & Authorization

        public async Task<bool> ValidateUserCredentialsAsync(string email, string password)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByEmailAsync(email);
                if (user == null || !user.IsActive || !user.IsEmailVerified)
                {
                    _logger.LogWarning("Login attempt failed for email {Email} - user not found, inactive, or unverified", email);
                    return false;
                }

                // Verify password using BCrypt
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                if (isPasswordValid)
                {
                    _logger.LogInformation("Successful login for user {Email}", email);
                }
                else
                {
                    _logger.LogWarning("Invalid password attempt for user {Email}", email);
                }

                return isPasswordValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for email {Email}", email);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for password change", userId);
                    return false;
                }

                // Validation
                if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid current password for user ID: {UserID}", userId);
                }

                // Hash new password
                string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword, 12);

                // Update user password
                user.PasswordHash = newPasswordHash;
                user.UpdatedAt = DateTime.UtcNow;
                user.PasswordResetRequired = false;

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Password changed successfully for user ID {UserId}", userId);
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user ID {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("User has email: {Email} not found", email);
                    return false;
                }

                // Generate temporary password
                string temporaryPassword = GenerateTemporaryPassword();
                string temporaryPasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword, 12);

                // Update user with temporary password
                user.PasswordHash = temporaryPasswordHash;
                user.UpdatedAt = DateTime.UtcNow;
                user.PasswordResetRequired = true; // Force password change

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Password reset completed for email {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for email {Email}", email);
                throw;
            }
        }

        public async Task<bool> VerifyEmailAsync(int userId, string verificationToken)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // TODO: Implement email verification logic
                user.IsEmailVerified = true;
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Email verified for user ID {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for user ID {UserId}", userId);
                throw;
            }

        }

        // User Status Management

        public async Task<bool> ActivateUserAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Activated user: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating User ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Deactivated user: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating User ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ToggleUserStatusAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("User ID {UserId} status toggled to {Status}", userId, user.IsActive ? "Active" : "Inactive");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for user ID {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<int>> ActivateUsersAsync(IEnumerable<int> userIds)
        {
            try
            {
                await _unitOfWork.Users.ActivateUsersAsync(userIds);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Bulk activation completed for {Count} users", userIds.Count());
                return userIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk user activation");
                throw;
            }
        }

        public async Task<IEnumerable<int>> DeactivateUsersAsync(IEnumerable<int> userIds)
        {
            try
            {
                await _unitOfWork.Users.DeactivateUsersAsync(userIds);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Bulk activation completed for {Count} users", userIds.Count());
                return userIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk user deactivation");
                throw;
            }
        }

        // Role Management

        public async Task<bool> ChangeUserRoleAsync(int userId, UserRole newRole)
        {
            try
            {
                if (!await CanUserRoleBeChangedAsync(userId, newRole))
                {
                    _logger.LogWarning("User ID {UserId} role cannot be changed to {Role} due to business rules", userId, newRole);
                    return false;
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                var oldRole = user.Role;
                if (oldRole != newRole)
                {
                    user.Role = newRole;
                    user.UpdatedAt = DateTime.UtcNow;
                }

                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("User ID {UserId} role changed from {OldRole} to {NewRole}", userId, oldRole, newRole);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing role for user ID {UserId} to {Role}", userId, newRole);
                throw;
            }
        }

        public async Task<IEnumerable<UserListDto>> GetUsersByRoleAsync(UserRole role)
        {
            try
            {
                var users = await _unitOfWork.Users.GetUsersByRoleAsync(role);

                if (users == null)
                {
                    return null;
                }

                return _mapper.Map<IEnumerable<UserListDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role {Role}", role);
                throw;
            }
        }

        public async Task<IEnumerable<UserListDto>> GetActiveUsersByRoleAsync(UserRole role)
        {
            try
            {
                var users = await _unitOfWork.Users.GetActiveUsersByRoleAsync(role);

                if (users == null)
                {
                    return null;
                }

                return _mapper.Map<IEnumerable<UserListDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role {Role}", role);
                throw;
            }
        }

        // Query Operations

        public async Task<IEnumerable<UserListDto>> GetActiveUsersAsync()
        {
            try
            {
                var users = await _unitOfWork.Users.GetActiveUsersAsync();
                return _mapper.Map<IEnumerable<UserListDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active users");
                throw;
            }
        }
        public async Task<IEnumerable<UserListDto>> GetInactiveUsersAsync()
        {
            try
            {
                var users = await _unitOfWork.Users.GetInactiveUsersAsync();
                return _mapper.Map<IEnumerable<UserListDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inactive users");
                throw;
            }
        }
        public async Task<IEnumerable<UserListDto>> GetUnverifiedUsersAsync()
        {
            try
            {
                var users = await _unitOfWork.Users.GetUnverifiedUsersAsync();
                return _mapper.Map<IEnumerable<UserListDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unverified users");
                throw;
            }
        }

        public async Task<IEnumerable<UserListDto>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                var users = await _unitOfWork.Users.SearchUsersAsync(searchTerm);

                return _mapper.Map<IEnumerable<UserListDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<UserListDto>> SearchUsersAsync(string searchTerm, int pageNumber, int pageSize)
        {
            try
            {
                var users = await _unitOfWork.Users.SearchUsersAsync(searchTerm, pageNumber, pageSize);
                return _mapper.Map<IEnumerable<UserListDto>>(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term {SearchTerm}, page {PageNumber}", searchTerm, pageNumber);
                throw;
            }
        }

        // Pagination

        public async Task<(IEnumerable<UserListDto> Users, int TotalCount)> GetUsersPagedAsync(int pageNumber, int pageSize)
        {
            try
            {
                var users = await _unitOfWork.Users.GetPagedAsync(pageNumber, pageSize);
                var totalCount = await _unitOfWork.Users.CountAsync();

                return (_mapper.Map<IEnumerable<UserListDto>>(users), totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged users");
                throw;
            }
        }

        public async Task<(IEnumerable<UserListDto> Users, int TotalCount)> GetUsersPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, UserRole? role = null, bool? isActive = null)
        {
            try
            {
                Expression<Func<User, bool>>? predicate = null;

                if (!string.IsNullOrEmpty(searchTerm) || role.HasValue || isActive.HasValue)
                {
                    predicate = u =>
                        (string.IsNullOrEmpty(searchTerm) ||
                         u.FirstName.Contains(searchTerm) ||
                         u.LastName.Contains(searchTerm) ||
                         u.Email.Contains(searchTerm)) &&
                        (!role.HasValue || u.Role == role.Value) &&
                        (!isActive.HasValue || u.IsActive == isActive.Value);
                }
                var users = await _unitOfWork.Users.GetPagedAsync(pageNumber, pageSize, predicate);
                var totalCount = await _unitOfWork.Users.CountAsync(predicate);

                return (_mapper.Map<IEnumerable<UserListDto>>(users), totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged users");
                throw;
            }
        }

        // Statistics

        public async Task<UserSummaryDto> GetUserSummaryAsync()
        {
            try
            {
                var totalUsers = await _unitOfWork.Users.CountAsync();
                var activeUsers = await _unitOfWork.Users.GetActiveUsersCountAsync();
                var inactiveUsers = totalUsers - activeUsers;
                var unverifiedUsers = await _unitOfWork.Users.CountAsync(u => !u.IsEmailVerified && u.IsActive);
                var usersByRole = await _unitOfWork.Users.GetUsersCountByRolesAsync();

                return new UserSummaryDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    InactiveUsers = inactiveUsers,
                    UnverifiedUsers = unverifiedUsers,
                    UsersByRole = usersByRole
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user summary");
                throw;
            }
        }

        public Task<int> GetTotalUsersCountAsync()
        {

            var totalUsers = _unitOfWork.Users.CountAsync();

            return totalUsers;

        }

        public async Task<int> GetActiveUsersCountAsync()
        {
            return await _unitOfWork.Users.GetActiveUsersCountAsync();
        }

        public async Task<int> GetUserCountByRoleAsync(UserRole role)
        {
            return await _unitOfWork.Users.GetUserCountByRoleAsync(role);
        }

        public async Task<Dictionary<UserRole, int>> GetUsersCountByRolesAsync()
        {
            return await _unitOfWork.Users.GetUsersCountByRolesAsync();
        }

        // Recent Activities
        public async Task<IEnumerable<UserListDto>> GetRecentlyRegisteredUsersAsync(int count = 10)
        {
            var users = await _unitOfWork.Users.GetRecentlyRegisteredUsersAsync(count);

            return _mapper.Map<IEnumerable<UserListDto>>(users);
        }

        public async Task<IEnumerable<UserListDto>> GetUsersRegisteredBetweenAsync(DateTime start, DateTime end)
        {
            var users = await _unitOfWork.Users.GetUsersRegisteredBetweenAsync(start, end);

            return _mapper.Map<IEnumerable<UserListDto>>(users);
        }

        public async Task<IEnumerable<UserListDto>> GetUsersRegisteredThisMonthAsync()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);
            return await GetUsersRegisteredBetweenAsync(startOfMonth, endOfMonth);
        }

        public async Task<IEnumerable<UserListDto>> GetUsersRegisteredThisWeekAsync()
        {
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);
            return await GetUsersRegisteredBetweenAsync(startOfWeek, endOfWeek);
        }

        public async Task<IEnumerable<UserListDto>> GetUsersRegisteredTodayAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await GetUsersRegisteredBetweenAsync(today, tomorrow);

        }


        // Bulk Operations

        public Task<bool> BulkDeleteUsersAsync(IEnumerable<int> ids)
        {
            throw new NotImplementedException();
        }

        public Task<bool> BulkUpdateUsersAsync(IEnumerable<int> ids, bool? isActive = null, UserRole? userRole = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserListDto>> GetUsersByIdsAsync(IEnumerable<int> ids)
        {
            throw new NotImplementedException();
        }


        // Validation & Business Rules

        public async Task<bool> CanUserBeDeletedAsync(int userId)
        {
            try
            {
                
                var user = await _unitOfWork.Users.GetUserWithOrdersAsync(userId);
                if (user?.Orders?.Any() == true)
                {
                    return false; // User has orders, cannot be deleted
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can be deleted {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> CanUserRoleBeChangedAsync(int userId, UserRole role)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

               
                if (user.Role == UserRole.SuperAdmin && role != UserRole.SuperAdmin)
                {
                   
                    var superAdminCount = await _unitOfWork.Users.GetUserCountByRoleAsync(UserRole.SuperAdmin);
                    if (superAdminCount <= 1)
                    {
                        return false; 
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user role can be changed {UserId} to {Role}", userId, role);
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(int userId)
        {
            try
            {
                return await _unitOfWork.Users.ExistsAsync(userId);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error checking if user exists: {UserId}", userId);
                throw;
            }
        }



        // Export methods 
        public Task<string> ExportUsersToCsvAsync()
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ExportUsersToExcelAsync()
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ExportUsersToExcelAsync(UserRole? role = null, bool? isActive = null)
        {
            throw new NotImplementedException();
        }

    }
}
