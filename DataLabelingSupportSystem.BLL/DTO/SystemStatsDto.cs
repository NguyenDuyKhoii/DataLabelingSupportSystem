using System;
using System.Collections.Generic;

namespace DataLabelingSupportSystem.BLL.DTO
{
    public class SystemStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalProjects { get; set; }
        public int TotalDataItems { get; set; }
        public int TotalAnnotations { get; set; }

        public List<RoleCountDto> UserDistribution { get; set; } = new();
        public List<ProjectStatusCountDto> ProjectDistribution { get; set; } = new();
        public List<ActivityLogDto> RecentActivities { get; set; } = new();
    }

    public class RoleCountDto
    {
        public string RoleName { get; set; } = null!;
        public int Count { get; set; }
    }

    public class ProjectStatusCountDto
    {
        public string Status { get; set; } = null!;
        public int Count { get; set; }
    }

    public class ActivityLogDto
    {
        public string Message { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; } = "bi-app-indicator";
        public string Color { get; set; } = "primary";
    }
}
