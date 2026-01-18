using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Models
{
    public class Enums
    {
        public enum TaskStatus { Assigned = 0, InProgress = 1, Completed = 2 }
        public enum SubmissionStatus { Submitted = 0, InReview = 1, Approved = 2, Rejected = 3 }
        public enum ReviewDecision { Approved = 0, Rejected = 1 }
    }
}
