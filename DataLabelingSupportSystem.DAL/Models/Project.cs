using DataLabelingSupportSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Models
{
    public class Project
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public int ManagerId { get; set; }
        public User Manager { get; set; } = null!;

        public ICollection<DataItem> DataItems { get; set; } = new List<DataItem>();
        public ICollection<Label> Labels { get; set; } = new List<Label>();
        public ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();
    }
}
