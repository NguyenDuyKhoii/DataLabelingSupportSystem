using DataLabelingSupportSystem.DAL.Interfaces;
using DataLabelingSupportSystem.DAL.Models;
using DataLabelingSupportSystem.DTOs;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Identity;

namespace DataLabelingSupportSystem.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<List<UserDto>> GetUsersAsync(string? search = null, int? roleId = null, bool? isActive = null)
        {
            // 1. Lấy dữ liệu thô từ Repo
            var users = await _userRepository.GetAllUsersAsync();

            // 2. Map đầy đủ dữ liệu sang DTO
            var userDtos = users.Select(u => new UserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Name = u.Name ?? "Not updated", // Handle null gracefully
                Email = u.Email ?? "",
                Phone = u.Phone ?? "",

                RoleId = u.RoleId,
                // Note: Ensure Repo has Include(u => u.Role) 
                // otherwise u.Role will be null -> causing error.
                RoleName = u.Role != null ? u.Role.RoleName : "Unknown",

                // IMPORTANT: Must map IsActive
                IsActive = u.IsActive,

                CreatedAt = u.CreatedAt
            }).ToList();

            return userDtos;
        }



        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return null;

            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Name = user.Name ?? "",
                Email = user.Email ?? "",
                Phone = user.Phone ?? "",
                RoleId = user.RoleId,
                RoleName = user.Role.RoleName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UpdateUserDto?> GetUserForUpdateAsync(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null) return null;

            return new UpdateUserDto
            {
                UserId = user.UserId,
                Name = user.Name ?? "",
                Email = user.Email ?? "",
                Phone = user.Phone ?? "",
                RoleId = user.RoleId,
                IsActive = user.IsActive
            };
        }

        public async Task<bool> CreateUserAsync(CreateUserDto dto)
        {
            // Validate username
            if (await _userRepository.UsernameExistsAsync(dto.Username))
                throw new InvalidOperationException("Username already exists. Please choose another username.");

            // Validate email if provided
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (await _userRepository.EmailExistsAsync(dto.Email))
                    throw new InvalidOperationException("Email is already in use. Please use another email.");
            }

            var user = new User
            {
                Username = dto.Username,
                Name = dto.Name,
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
                RoleId = dto.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
 
            user.Password = _passwordHasher.HashPassword(user, dto.Password);

            await _userRepository.AddAsync(user);
            return true;
        }



        public async Task<bool> UpdateUserAsync(UpdateUserDto dto)
        {
            var user = await _userRepository.GetUserByIdAsync(dto.UserId);
            if (user == null)
                throw new InvalidOperationException("User to be updated not found.");

            // Validate email if provided and changed
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var trimmedEmail = dto.Email.Trim();
                if (user.Email != trimmedEmail)
                {
                    if (await _userRepository.EmailExistsAsync(trimmedEmail, dto.UserId))
                        throw new InvalidOperationException("This email is already in use. Please use another email.");
                }
            }

            // Update properties directly on the tracked entity
            user.Name = dto.Name ?? "";
            user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            user.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
            user.RoleId = dto.RoleId;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            // Save directly - entity is tracked so only SaveChanges is needed
            await _userRepository.SaveUserAsync(user);
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            await _userRepository.DeleteAsync(id);
            return true;
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
            => await _userRepository.EmailExistsAsync(email, excludeUserId);
    }
}
