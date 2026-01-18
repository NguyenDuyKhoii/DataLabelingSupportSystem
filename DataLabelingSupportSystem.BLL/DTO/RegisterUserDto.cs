using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.DTO
{
   public record RegisterUserDto(string Username, string Password, string? Name);
}
