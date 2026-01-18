using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Models
{
    public class DataItem
    {
        public int DataItemId { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public string ImagePath { get; set; } = null!;

        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    }
}
