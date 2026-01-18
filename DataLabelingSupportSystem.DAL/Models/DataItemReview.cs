using DataLabelingSupportSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataLabelingSupportSystem.DAL.Models.Enums;

namespace DataLabelingSupportSystem.DAL.Models
{
    public class DataItemReview
    {
        public int ReviewId { get; set; }

        public int DataItemSubmissionId { get; set; }
        public DataItemSubmission Submission { get; set; } = null!;

        public int ReviewerId { get; set; }
        public User Reviewer { get; set; } = null!;

        public ReviewDecision Decision { get; set; }
        public string? Comment { get; set; }
        public DateTime ReviewedAt { get; set; }
    }
}
