using DataLabelingSupportSystem.BLL.DTO;

namespace DataLabelingSupportSystem.BLL.DTO
{
    public class AnnotateContextDto
    {
        public DataItemDto Item { get; set; } = null!;
        public int SubmissionId { get; set; }
        public string? Color { get; set; }
        public string? SubmissionStatus { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public bool CanEdit { get; set; }

        public List<LabelViewDto> Labels { get; set; } = new();
        public string? ReviewComment { get; set; }
    }
}