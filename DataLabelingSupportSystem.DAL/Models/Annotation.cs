using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Models
{
    public class Annotation
    {
        public int AnnotationId { get; set; }

        public int DataItemSubmissionId { get; set; }
        public DataItemSubmission Submission { get; set; } = null!;

        public int LabelId { get; set; }
        public Label Label { get; set; } = null!;

        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }
}
