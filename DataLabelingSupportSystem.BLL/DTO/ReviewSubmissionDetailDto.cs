using System;
using System.Collections.Generic;

namespace DataLabelingSupportSystem.BLL.DTO
{
    public class ReviewSubmissionDetailDto
    {
        public int SubmissionId { get; set; }
        public int TaskItemId { get; set; }

        public int SubmittedBy { get; set; }
        public string SubmittedByName { get; set; } = "";
        public DateTime SubmittedAtUtc { get; set; }
        public string Status { get; set; } = "";

        // For UI
        public string ImagePath { get; set; } = "";

        // Read-only boxes (normalized 0..1)
        public List<AnnotationBoxDto> Boxes { get; set; } = new();

        public Dictionary<int, string> LabelColors { get; set; } = new();
    }
}