using DataLabelingSupportSystem.DAL.DbContext;
using DataLabelingSupportSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Interfaces
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;
        public UserRepository(AppDbContext db) => _db = db;

        // Authentication (keep as is)
        public Task<User?> GetByUsernameAsync(string username)
            => _db.Users.Include(u => u.Role)
                       .FirstOrDefaultAsync(u => u.Username == username);

        public Task<bool> UsernameExistsAsync(string username)
            => _db.Users.AnyAsync(x => x.Username == username);

        public async Task AddAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        // ADD CRUD methods
        public async Task<List<User>> GetAllUsersAsync()
            => await _db.Users
                       .Include(u => u.Role)
                       .OrderByDescending(u => u.CreatedAt)
                       .ToListAsync();

        public async Task<User?> GetUserByIdAsync(int id)
            => await _db.Users.Include(u => u.Role)
                             .FirstOrDefaultAsync(u => u.UserId == id);

        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
        {
            var query = _db.Users.Where(u => u.Email == email);
            if (excludeUserId.HasValue)
                query = query.Where(u => u.UserId != excludeUserId.Value);
            return await query.AnyAsync();
        }

        public async Task UpdateAsync(User user)
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            if (existingUser != null)
            {
                existingUser.Name = user.Name;
                existingUser.Email = user.Email;
                existingUser.Phone = user.Phone;
                existingUser.RoleId = user.RoleId;
                existingUser.IsActive = user.IsActive;
                existingUser.UpdatedAt = user.UpdatedAt;
                await _db.SaveChangesAsync();
            }
        }

        public async Task SaveUserAsync(User user)
        {
            // Entity is already tracked from GetUserByIdAsync, just save
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id) // Soft delete
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user != null)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<int> CountAsync()
            => await _db.Users.CountAsync();
    }
}
