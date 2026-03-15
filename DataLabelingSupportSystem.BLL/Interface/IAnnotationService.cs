using DataLabelingSupportSystem.BLL.DTO;

namespace DataLabelingSupportSystem.BLL.Interface
{
    public interface IAnnotationService
    {
        Task<AnnotateContextDto?> GetAnnotateContextAsync(int taskItemId, int annotatorId);

        Task<int> SaveAnnotationsAsync(int taskItemId, int annotatorId, List<AnnotationBoxDto> boxes);

        Task<List<AnnotationBoxDto>> GetSavedBoxesAsync(int taskItemId, int annotatorId);

        Task SubmitAsync(int taskItemId, int annotatorId);

        Task SetInReviewAsync(int dataItemSubmissionId, int reviewerId);
        Task ApproveAsync(int dataItemSubmissionId, int reviewerId, string? comment);
        Task RejectAsync(int dataItemSubmissionId, int reviewerId, string? comment);

        Task<List<ReviewQueueItemDto>> GetReviewQueueAsync(int reviewerId);

        Task<ReviewSubmissionDetailDto> GetReviewSubmissionDetailAsync(int dataItemSubmissionId, int reviewerId);
        Task<ProjectYoloExportDto?> GetProjectYoloExportAsync(int projectId);
        Task<int?> GetNextTaskItemIdAsync(int currentTaskItemId, int annotatorId);
        Task<int?> GetNextReviewSubmissionIdAsync(int currentSubmissionId);
    }
}
