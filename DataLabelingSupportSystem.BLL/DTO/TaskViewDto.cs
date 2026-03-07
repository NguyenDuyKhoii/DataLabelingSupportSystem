using System;

namespace DataLabelingSupportSystem.BLL.DTO
{
    public class TaskViewDto
    {
        public int TaskId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public int AnnotatorId { get; set; }
        public string AnnotatorName { get; set; } = null!;
        public DataLabelingSupportSystem.DAL.Models.Enums.TaskStatus Status { get; set; }
        public int ImageCount { get; set; }


        public int SubmittedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
    }
}
