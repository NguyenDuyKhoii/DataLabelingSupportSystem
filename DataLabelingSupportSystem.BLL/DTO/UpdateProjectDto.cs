using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.DTO
{
    public class UpdateProjectDto
    {
        public int Id { get; set; }

        
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

       
        public string Status { get; set; } = "New"; 
    }
}
