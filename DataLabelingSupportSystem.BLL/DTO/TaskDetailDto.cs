using System.Collections.Generic;
using static DataLabelingSupportSystem.DAL.Models.Enums;

namespace DataLabelingSupportSystem.BLL.DTO
{
    public class TaskDetailDto
    {
        public int TaskId { get; set; }
        public string ProjectName { get; set; } = null!;
        public DataLabelingSupportSystem.DAL.Models.Enums.TaskStatus Status { get; set; }
        public List<DataItemDto> Items { get; set; } = new List<DataItemDto>();
    }
}
