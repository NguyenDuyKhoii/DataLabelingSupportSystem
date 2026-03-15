using System;
using System.Collections.Generic;

namespace DataLabelingSupportSystem.BLL.DTO
{
    public class ProjectStatsDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int TotalImages { get; set; }
        public int ApprovedImages { get; set; }
        public int RejectedImages { get; set; }
        public int PendingImages { get; set; } // Submitted but not reviewed
        public int UnassignedImages { get; set; }

        // For Charts
        public List<LabelCountDto> LabelDistribution { get; set; } = new();
        public List<DailyProgressDto> DailyProgress { get; set; } = new();
    }

    public class LabelCountDto
    {
        public string LabelName { get; set; }
        public int Count { get; set; }
        public string Color { get; set; }
    }

    public class DailyProgressDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class ProjectYoloExportDto
    {
        public string ProjectName { get; set; }
        public List<YoloItemDto> Items { get; set; } = new();
        public List<string> LabelNamesByOrder { get; set; } = new();
    }

    public class YoloItemDto
    {
        public string FileName { get; set; }
        public string ImagePath { get; set; }
        public List<string> YoloLines { get; set; } = new();
    }
}
