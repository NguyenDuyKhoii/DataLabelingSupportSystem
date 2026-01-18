using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.DTO
{
    public record AuthUserDto(int UserId, string Username, string? Name, string RoleName);
}
