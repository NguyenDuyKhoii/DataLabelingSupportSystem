using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.DTO
{
    public class TaskProgressDTO
    {
        public int TaskId { get; set; }
        public string AnnotatorName { get; set; } = string.Empty;
        public int TotalItems { get; set; }

        public int SubmittedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }

       
        public double PercentSubmitted => TotalItems > 0 ? Math.Round((double)SubmittedCount / TotalItems * 100, 2) : 0;
        public double PercentApproved => TotalItems > 0 ? Math.Round((double)ApprovedCount / TotalItems * 100, 2) : 0;
        public double PercentRejected => TotalItems > 0 ? Math.Round((double)RejectedCount / TotalItems * 100, 2) : 0;
    }
}
