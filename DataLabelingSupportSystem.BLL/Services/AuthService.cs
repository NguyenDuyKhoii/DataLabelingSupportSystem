using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.DAL.Interfaces;
using DataLabelingSupportSystem.DAL.Models;
using Microsoft.AspNetCore.Identity;

namespace DataLabelingSupportSystem.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher<User> _hasher;

        public AuthService(IUserRepository users, IPasswordHasher<User> hasher)
        {
            _users = users;
            _hasher = hasher;
        }

        public async Task<AuthUserDto?> LoginAsync(string username, string password)
        {
            var user = await _users.GetByUsernameAsync(username);
            if (user is null) return null;

            var result = _hasher.VerifyHashedPassword(user, user.Password, password);
            if (result == PasswordVerificationResult.Failed) return null;

            return new AuthUserDto(
                user.UserId,
                user.Username,
                user.Name,
                user.Role.RoleName
            );
        }

        public async Task<AuthUserDto> RegisterAsync(RegisterUserDto req)
        {
            if (await _users.UsernameExistsAsync(req.Username))
                throw new InvalidOperationException("Username already exists.");

            const int defaultRoleId = 3;          // Annotator (per your DB)
            const string defaultRoleName = "Annotator"; // for DTO return

            var user = new User
            {
                Username = req.Username,
                Name = req.Name,
                RoleId = defaultRoleId
            };

            user.Password = _hasher.HashPassword(user, req.Password); 

            await _users.AddAsync(user); // repo handles SaveChanges

            return new AuthUserDto(user.UserId, user.Username, user.Name, defaultRoleName);
        }
    }
}
