using DataLabelingSupportSystem.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.Interface
{
    public interface IAuthService
    {
        Task<AuthUserDto?> LoginAsync(string username, string password);

        Task<AuthUserDto> RegisterAsync(RegisterUserDto req);
    }
}
