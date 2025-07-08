using Microsoft.AspNetCore.Mvc;
using ProductAPI.Business.DTOs.User;
using ProductAPI.Business.Services.Interfaces;
using ProductAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductAPI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns>List of users</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserListDto>>> GetUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }

        /// <summary>
        /// Get users with pagination
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <param name="searchTerm">Search term for filtering</param>
        /// <param name="role">Filter by role</param>
        /// <param name="isActive">Filter by active status</param>
        /// <returns>Paginated list of users</returns>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetUsersPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] UserRole? role = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var (users, totalCount) = await _userService.GetUsersPagedAsync(
                    pageNumber, pageSize, searchTerm, role, isActive);

                var response = new
                {
                    Data = users,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasNextPage = pageNumber * pageSize < totalCount,
                    HasPreviousPage = pageNumber > 1
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged users");
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid user ID");
                }

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        /// <summary>
        /// Get user profile by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User profile</returns>
        [HttpGet("{id}/profile")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile(int id)
        {
            try
            {
                var userProfile = await _userService.GetUserProfileByIdAsync(id);
                if (userProfile == null)
                {
                    return NotFound($"User profile with ID {id} not found");
                }

                return Ok(userProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile {UserId}", id);
                return StatusCode(500, "An error occurred while retrieving the user profile");
            }
        }

        /// <summary>
        /// Get user with orders
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="takeOrdersCount">Number of recent orders to include</param>
        /// <returns>User with orders</returns>
        [HttpGet("{id}/with-orders")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUserWithOrders(int id, [FromQuery] int? takeOrdersCount = null)
        {
            try
            {
                UserDto? user;

                if (takeOrdersCount.HasValue)
                {
                    user = await _userService.GetUserWithOrdersAsync(id, takeOrdersCount.Value);
                }
                else
                {
                    user = await _userService.GetUserWithOrdersAsync(id);
                }

                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with orders {UserId}", id);
                return StatusCode(500, "An error occurred while retrieving the user with orders");
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="createUserDto">User creation data</param>
        /// <returns>Created user</returns>
        [HttpPost]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if email is available
                if (!await _userService.IsEmailAvailableAsync(createUserDto.Email))
                {
                    return BadRequest($"Email {createUserDto.Email} is already in use");
                }

                var user = await _userService.CreateUserAsync(createUserDto);
                return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating user");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "An error occurred while creating the user");
            }
        }

        /// <summary>
        /// Update user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="updateUserDto">User update data</param>
        /// <returns>Updated user</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if email is available (excluding current user)
                if (!await _userService.IsEmailAvailableAsync(updateUserDto.Email, id))
                {
                    return BadRequest($"Email {updateUserDto.Email} is already in use by another user");
                }

                var user = await _userService.UpdateUserAsync(id, updateUserDto);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating user {UserId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, "An error occurred while updating the user");
            }
        }

        /// <summary>
        /// Delete user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>No content if successful</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                if (!await _userService.UserExistsAsync(id))
                {
                    return NotFound($"User with ID {id} not found");
                }

                if (!await _userService.CanUserBeDeletedAsync(id))
                {
                    return Conflict("User cannot be deleted due to existing orders or other dependencies");
                }

                var result = await _userService.DeleteUserAsync(id);
                if (!result)
                {
                    return NotFound($"User with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="changePasswordDto">Password change data</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _userService.ChangePasswordAsync(id, changePasswordDto);
                if (!result)
                {
                    return BadRequest("Invalid current password or user not found");
                }

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", id);
                return StatusCode(500, "An error occurred while changing the password");
            }
        }

        /// <summary>
        /// Activate user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateUser(int id)
        {
            try
            {
                var result = await _userService.ActivateUserAsync(id);
                if (!result)
                {
                    return NotFound($"User with ID {id} not found");
                }

                return Ok(new { message = "User activated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user {UserId}", id);
                return StatusCode(500, "An error occurred while activating the user");
            }
        }

        /// <summary>
        /// Deactivate user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var result = await _userService.DeactivateUserAsync(id);
                if (!result)
                {
                    return NotFound($"User with ID {id} not found");
                }

                return Ok(new { message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                return StatusCode(500, "An error occurred while deactivating the user");
            }
        }

        /// <summary>
        /// Toggle user status
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            try
            {
                var result = await _userService.ToggleUserStatusAsync(id);
                if (!result)
                {
                    return NotFound($"User with ID {id} not found");
                }

                return Ok(new { message = "User status toggled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for user {UserId}", id);
                return StatusCode(500, "An error occurred while toggling user status");
            }
        }

        /// <summary>
        /// Change user role
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="newRole">New role</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/change-role")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] UserRole newRole)
        {
            try
            {
                if (!await _userService.CanUserRoleBeChangedAsync(id, newRole))
                {
                    return BadRequest("User role cannot be changed due to business rules");
                }

                var result = await _userService.ChangeUserRoleAsync(id, newRole);
                if (!result)
                {
                    return NotFound($"User with ID {id} not found");
                }

                return Ok(new { message = $"User role changed to {newRole} successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing role for user {UserId}", id);
                return StatusCode(500, "An error occurred while changing user role");
            }
        }

        /// <summary>
        /// Search users
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Search results</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<UserListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserListDto>>> SearchUsers(
            [FromQuery] string searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest("Search term is required");
                }

                IEnumerable<UserListDto> users;

                if (pageNumber > 1 || pageSize != 10)
                {
                    users = await _userService.SearchUsersAsync(searchTerm, pageNumber, pageSize);
                }
                else
                {
                    users = await _userService.SearchUsersAsync(searchTerm);
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term {SearchTerm}", searchTerm);
                return StatusCode(500, "An error occurred while searching users");
            }
        }

        /// <summary>
        /// Get users by role
        /// </summary>
        /// <param name="role">User role</param>
        /// <param name="activeOnly">Include only active users</param>
        /// <returns>Users with specified role</returns>
        [HttpGet("by-role/{role}")]
        [ProducesResponseType(typeof(IEnumerable<UserListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserListDto>>> GetUsersByRole(
            UserRole role,
            [FromQuery] bool activeOnly = false)
        {
            try
            {
                IEnumerable<UserListDto> users;

                if (activeOnly)
                {
                    users = await _userService.GetActiveUsersByRoleAsync(role);
                }
                else
                {
                    users = await _userService.GetUsersByRoleAsync(role);
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users by role {Role}", role);
                return StatusCode(500, "An error occurred while retrieving users by role");
            }
        }

        /// <summary>
        /// Get user statistics
        /// </summary>
        /// <returns>User summary statistics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserSummaryDto>> GetUserStatistics()
        {
            try
            {
                var summary = await _userService.GetUserSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user statistics");
                return StatusCode(500, "An error occurred while retrieving user statistics");
            }
        }

        /// <summary>
        /// Get recently registered users
        /// </summary>
        /// <param name="takeCount">Number of users to return</param>
        /// <returns>Recently registered users</returns>
        [HttpGet("recent")]
        [ProducesResponseType(typeof(IEnumerable<UserListDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserListDto>>> GetRecentUsers([FromQuery] int takeCount = 10)
        {
            try
            {
                if (takeCount < 1 || takeCount > 100)
                {
                    takeCount = 10;
                }

                var users = await _userService.GetRecentlyRegisteredUsersAsync(takeCount);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent users");
                return StatusCode(500, "An error occurred while retrieving recent users");
            }
        }

        /// <summary>
        /// Bulk activate users
        /// </summary>
        /// <param name="userIds">List of user IDs</param>
        /// <returns>Success status</returns>
        [HttpPost("bulk-activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkActivateUsers([FromBody] IEnumerable<int> userIds)
        {
            try
            {
                if (!userIds.Any())
                {
                    return BadRequest("User IDs are required");
                }

                var activatedUserIds = await _userService.ActivateUsersAsync(userIds);
                return Ok(new { message = $"{activatedUserIds.Count()} users activated successfully", userIds = activatedUserIds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk user activation");
                return StatusCode(500, "An error occurred while activating users");
            }
        }

        /// <summary>
        /// Bulk deactivate users
        /// </summary>
        /// <param name="userIds">List of user IDs</param>
        /// <returns>Success status</returns>
        [HttpPost("bulk-deactivate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkDeactivateUsers([FromBody] IEnumerable<int> userIds)
        {
            try
            {
                if (!userIds.Any())
                {
                    return BadRequest("User IDs are required");
                }

                var deactivatedUserIds = await _userService.DeactivateUsersAsync(userIds);
                return Ok(new { message = $"{deactivatedUserIds.Count()} users deactivated successfully", userIds = deactivatedUserIds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk user deactivation");
                return StatusCode(500, "An error occurred while deactivating users");
            }
        }
    }
}