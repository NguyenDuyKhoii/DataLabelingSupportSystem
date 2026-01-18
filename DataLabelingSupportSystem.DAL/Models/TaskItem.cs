using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Models
{
    public class TaskItem
    {
        public int TaskItemId { get; set; }

        public int TaskId { get; set; }
        public TaskEntity Task { get; set; } = null!;

        public int DataItemId { get; set; }
        public DataItem DataItem { get; set; } = null!;

        public ICollection<DataItemSubmission> Submissions { get; set; } = new List<DataItemSubmission>();
    }
}
