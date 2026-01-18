using DataLabelingSupportSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataLabelingSupportSystem.DAL.Models.Enums;


namespace DataLabelingSupportSystem.DAL.Models
{
    public class DataItemSubmission
    {
        public int DataItemSubmissionId { get; set; }

        public int TaskItemId { get; set; }
        public TaskItem TaskItem { get; set; } = null!;

        public int SubmittedBy { get; set; }
        public User Submitter { get; set; } = null!;

        public DateTime SubmittedAt { get; set; }
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

        public DataItemReview? Review { get; set; }
        public ICollection<Annotation> Annotations { get; set; } = new List<Annotation>();
    }
}
