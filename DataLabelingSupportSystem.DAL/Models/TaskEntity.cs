using DataLabelingSupportSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataLabelingSupportSystem.DAL.Models.Enums;

namespace DataLabelingSupportSystem.DAL.Models
{
    public class TaskEntity
    {
        public int TaskId { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public int AnnotatorId { get; set; }
        public User Annotator { get; set; } = null!;

        public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.Assigned;

        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    }
}
