using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Models
{
    public class Label
    {
        public int LabelId { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public string Name { get; set; } = null!;
        public string? Color { get; set; }

        public ICollection<Annotation> Annotations { get; set; } = new List<Annotation>();
    }
}
