namespace DataLabelingSupportSystem.BLL.DTO
{
    public class DataItemDto
    {
        public int TaskItemId { get; set; }
        public int DataItemId { get; set; }
        public string ImagePath { get; set; } = null!;

        // New properties for status notification
        public DataLabelingSupportSystem.DAL.Models.Enums.SubmissionStatus? LatestStatus { get; set; }
        public int? LatestSubmissionId { get; set; }
        public string? ReviewComment { get; set; }
    }
}
