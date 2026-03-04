namespace DataLabelingSupportSystem.BLL.DTO
{
    public class ReviewQueueItemDto
    {
        public int SubmissionId { get; set; }
        public int TaskItemId { get; set; }
        public int SubmittedBy { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public string Status { get; set; } = "";
    }
}