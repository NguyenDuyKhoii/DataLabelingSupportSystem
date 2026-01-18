using DataLabelingSupportSystem.DAL.DbContext;
using DataLabelingSupportSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Interfaces
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;
        public UserRepository(AppDbContext db) => _db = db;

        public Task<User?> GetByUsernameAsync(string username)
            => _db.Users
                  .Include(u => u.Role)
                  .FirstOrDefaultAsync(u => u.Username == username);

        public Task<bool> UsernameExistsAsync(string username)
        => _db.Users.AnyAsync(x => x.Username == username);

        public async Task AddAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
    }
}
