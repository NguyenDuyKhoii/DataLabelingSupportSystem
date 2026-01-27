using DataLabelingSupportSystem.DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<bool> UsernameExistsAsync(string username);
        Task AddAsync(User user);

        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
        Task UpdateAsync(User user);
        Task SaveUserAsync(User user);
        Task DeleteAsync(int id);
        Task<int> CountAsync();
    }
}
