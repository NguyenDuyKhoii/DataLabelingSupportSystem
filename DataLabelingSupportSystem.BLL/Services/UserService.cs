using DataLabelingSupportSystem.DAL.Interfaces;
using DataLabelingSupportSystem.DAL.Models;
using DataLabelingSupportSystem.DTOs;
using DataLabelingSupportSystem.BLL.Utilities;
using DataLabelingSupportSystem.BLL.Interface;

namespace DataLabelingSupportSystem.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
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
                Name = u.Name ?? "Chưa cập nhật", // Handle null cho đẹp
                Email = u.Email ?? "",
                Phone = u.Phone ?? "",

                RoleId = u.RoleId,
                // Lưu ý: Cần chắc chắn Repo đã Include(u => u.Role) 
                // nếu không u.Role sẽ null -> gây lỗi.
                RoleName = u.Role != null ? u.Role.RoleName : "Unknown",

                // QUAN TRỌNG NHẤT: Phải map IsActive
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
                throw new InvalidOperationException("Username đã tồn tại. Vui lòng chọn username khác.");

            // Validate email nếu có
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (await _userRepository.EmailExistsAsync(dto.Email))
                    throw new InvalidOperationException("Email này đã được sử dụng. Vui lòng sử dụng email khác.");
            }

            var user = new User
            {
                Username = dto.Username,
                Password = PasswordHelper.HashPassword(dto.Password),
                Name = dto.Name,
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
                RoleId = dto.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            return true;
        }



        public async Task<bool> UpdateUserAsync(UpdateUserDto dto)
        {
            var user = await _userRepository.GetUserByIdAsync(dto.UserId);
            if (user == null)
                throw new InvalidOperationException("Không tìm thấy user cần cập nhật.");

            // Validate email nếu có và đã thay đổi
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var trimmedEmail = dto.Email.Trim();
                if (user.Email != trimmedEmail)
                {
                    if (await _userRepository.EmailExistsAsync(trimmedEmail, dto.UserId))
                        throw new InvalidOperationException("Email này đã được sử dụng. Vui lòng sử dụng email khác.");
                }
            }

            // Update properties trực tiếp trên entity đã được track
            user.Name = dto.Name ?? "";
            user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            user.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
            user.RoleId = dto.RoleId;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            // Lưu trực tiếp - entity đã được track nên chỉ cần SaveChanges
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
