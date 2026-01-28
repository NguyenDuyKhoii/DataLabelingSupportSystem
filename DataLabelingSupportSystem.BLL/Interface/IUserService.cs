using DataLabelingSupportSystem.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.Interface
{
    public interface IUserService
    {
        Task<List<UserDto>> GetUsersAsync(string? search = null, int? roleId = null, bool? isActive = null);
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UpdateUserDto?> GetUserForUpdateAsync(int id);
        Task<bool> CreateUserAsync(CreateUserDto dto);
        Task<bool> UpdateUserAsync(UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
    }
}
