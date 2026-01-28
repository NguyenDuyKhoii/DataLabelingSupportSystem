using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.DTO
{
    public class LabelViewDto
    {
        public int Id { get; set; }
        public string Name { get; set; }    
        public string Color { get; set; }
        public int ProjectId { get; set; }
    }
}
