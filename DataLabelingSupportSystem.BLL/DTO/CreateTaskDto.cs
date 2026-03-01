using System.Collections.Generic;

namespace DataLabelingSupportSystem.BLL.DTO
{
    public class CreateTaskDto
    {
        public int ProjectId { get; set; }
        public int AnnotatorId { get; set; }
        public List<int> DataItemIds { get; set; } = new List<int>();
    }
}
