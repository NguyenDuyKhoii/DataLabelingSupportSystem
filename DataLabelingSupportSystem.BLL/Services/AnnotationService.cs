using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.DAL.DbContext;
using DataLabelingSupportSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using static DataLabelingSupportSystem.DAL.Models.Enums;

namespace DataLabelingSupportSystem.BLL.Services
{
    public class AnnotationService : IAnnotationService
    {
        private readonly AppDbContext _db;
        private readonly ILabelService _labelService;

        public AnnotationService(AppDbContext db, ILabelService labelService)
        {
            _db = db;
            _labelService = labelService;
        }

        // Sentinel để biểu diễn "Saved but not submitted" mà không đổi schema
        // IMPORTANT: SQL Server/EF thường trả DateTime.Kind = Unspecified => không dùng ToUniversalTime() để so sánh sentinel.
        private static readonly DateTime SentinelSubmittedAt =
            new DateTime(1900, 1, 1, 0, 0, 0); // Kind: Unspecified

        private static bool IsSentinel(DateTime dt)
            => dt.Year == 1900 && dt.Month == 1 && dt.Day == 1
            && dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0;

        private async Task EnsureAnnotatorOwnsTaskItemAsync(int taskItemId, int annotatorId)
        {
            var ok = await _db.TaskItems
                .AnyAsync(ti => ti.TaskItemId == taskItemId && ti.Task.AnnotatorId == annotatorId);

            if (!ok)
                throw new UnauthorizedAccessException("Access denied for this TaskItem.");
        }
        // ---------------------------
        // Helpers: luôn làm việc với DRAFT (sentinel) để Save != Submit
        // ---------------------------
        private IQueryable<DataItemSubmission> SubmissionsOf(int taskItemId, int userId)
            => _db.DataItemSubmissions.Where(s => s.TaskItemId == taskItemId && s.SubmittedBy == userId);

        private Task<DataItemSubmission?> GetLatestDraftAsync(int taskItemId, int userId)
            => SubmissionsOf(taskItemId, userId)
                .Where(s => s.SubmittedAt == SentinelSubmittedAt)
                .OrderByDescending(s => s.DataItemSubmissionId)
                .FirstOrDefaultAsync();

        private async Task<DataItemSubmission> GetOrCreateDraftAsync(int taskItemId, int userId)
        {
            // Serializable để tránh 2 request đồng thời cùng "không thấy draft" rồi tạo 2 draft sentinel.
            await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var draft = await GetLatestDraftAsync(taskItemId, userId);
            if (draft != null)
            {
                await tx.CommitAsync();
                return draft;
            }

            // Rework: nếu submission mới nhất bị Rejected → tạo draft mới + copy annotations
            var submissions = await SubmissionsOf(taskItemId, userId)
                .OrderByDescending(s => s.DataItemSubmissionId)
                .ToListAsync();

            if (submissions.Any(s => s.Status == SubmissionStatus.Approved))
            {
                throw new InvalidOperationException("This item has been approved and cannot be edited.");
            }

            var latestRejected = submissions.FirstOrDefault(s => s.Status == SubmissionStatus.Rejected);

            var created = new DataItemSubmission
            {
                TaskItemId = taskItemId,
                SubmittedBy = userId,
                SubmittedAt = SentinelSubmittedAt,
                Status = SubmissionStatus.Submitted // draft phân biệt bằng sentinel
            };

            _db.DataItemSubmissions.Add(created);
            await _db.SaveChangesAsync();

            // Copy annotations từ submission bị reject để Annotator có điểm bắt đầu sửa
            if (latestRejected != null)
            {
                var oldAnnotations = await _db.Annotations
                    .Where(a => a.DataItemSubmissionId == latestRejected.DataItemSubmissionId)
                    .ToListAsync();

                if (oldAnnotations.Count > 0)
                {
                    var copied = oldAnnotations.Select(a => new Annotation
                    {
                        DataItemSubmissionId = created.DataItemSubmissionId,
                        LabelId = a.LabelId,
                        X = a.X,
                        Y = a.Y,
                        Width = a.Width,
                        Height = a.Height
                    });

                    _db.Annotations.AddRange(copied);
                    await _db.SaveChangesAsync();
                }
            }

            await tx.CommitAsync();
            return created;
        }

        public async Task<AnnotateContextDto?> GetAnnotateContextAsync(int taskItemId, int annotatorId)
        {
            var taskItem = await _db.TaskItems
                .Include(x => x.Task)
                .Include(x => x.DataItem)
                .Include(x => x.Submissions)
                .FirstOrDefaultAsync(x => x.TaskItemId == taskItemId);

            if (taskItem == null) return null;
            await EnsureAnnotatorOwnsTaskItemAsync(taskItemId, annotatorId);
            // IMPORTANT:
            // Context ưu tiên "draft hiện tại" (Saved-but-not-submitted).
            // Nếu không có draft thì fallback sang submission mới nhất để UI không bị rỗng sau khi Submit.
            var draft = taskItem.Submissions
                .Where(s => s.SubmittedBy == annotatorId && s.SubmittedAt == SentinelSubmittedAt)
                .OrderByDescending(s => s.DataItemSubmissionId)
                .FirstOrDefault();

            var latest = taskItem.Submissions
                .Where(s => s.SubmittedBy == annotatorId)
                .OrderByDescending(s => s.DataItemSubmissionId)
                .FirstOrDefault();

            var display = draft ?? latest;

            // NOTE (flow chuẩn theo schema + yêu cầu):
            // - KHÔNG tạo DataItemSubmission khi mở trang (GET).
            // - Submission chỉ tạo khi Save/Submit lần đầu (POST).
            var labels = await _labelService.GetLabelsByProjectIdAsync(taskItem.Task.ProjectId);

            bool canEdit;
            if (display == null)
            {
                canEdit = true;
            }
            else
            {
                // Khi đã vào review/final thì khóa edit để tránh sửa ngược luồng
                // Rejected → cho phép rework (canEdit = true)
                canEdit = display.Status != SubmissionStatus.InReview
                          && display.Status != SubmissionStatus.Approved;
            }

            // Load review comment nếu submission mới nhất bị Rejected (hiển thị lý do cho Annotator)
            string? reviewComment = null;
            if (display != null && display.Status == SubmissionStatus.Rejected)
            {
                var review = await _db.DataItemReviews
                    .Where(r => r.DataItemSubmissionId == display.DataItemSubmissionId)
                    .FirstOrDefaultAsync();
                reviewComment = review?.Comment;
            }

            return new AnnotateContextDto
            {
                Item = new DataItemDto
                {
                    TaskItemId = taskItem.TaskItemId,
                    DataItemId = taskItem.DataItemId,
                    ImagePath = taskItem.DataItem.ImagePath
                },
                SubmissionId = display?.DataItemSubmissionId ?? 0,
                SubmissionStatus = display?.Status.ToString(),
                SubmittedAt = display?.SubmittedAt,
                CanEdit = canEdit,
                Labels = labels,
                ReviewComment = reviewComment
            };
        }

        public async Task<int> SaveAnnotationsAsync(int taskItemId, int annotatorId, List<AnnotationBoxDto> boxes)
        {
            if (boxes == null) boxes = new List<AnnotationBoxDto>();

            // 1) Load TaskItem + ProjectId (để check Label thuộc project)
            var taskItem = await _db.TaskItems
                .Include(ti => ti.DataItem)
                .ThenInclude(di => di.Project)
                .FirstOrDefaultAsync(ti => ti.TaskItemId == taskItemId);

            if (taskItem == null)
                throw new InvalidOperationException("TaskItem not found.");
            await EnsureAnnotatorOwnsTaskItemAsync(taskItemId, annotatorId);
            var projectId = taskItem.DataItem.ProjectId;

            // 2) Validate boxes: normalized 0..1 and inside image
            foreach (var b in boxes)
            {
                if (b.Width <= 0 || b.Height <= 0)
                    throw new InvalidOperationException("BBox width/height must be > 0.");

                if (b.X < 0 || b.Y < 0 || b.Width < 0 || b.Height < 0)
                    throw new InvalidOperationException("BBox values must be >= 0.");

                if (b.X > 1 || b.Y > 1 || b.Width > 1 || b.Height > 1)
                    throw new InvalidOperationException("BBox values must be <= 1.");

                if (b.X + b.Width > 1.00001f || b.Y + b.Height > 1.00001f)
                    throw new InvalidOperationException("BBox must not exceed image bounds.");
            }

            // 3) Validate labels belong to same project
            var labelIds = boxes.Select(x => x.LabelId).Distinct().ToList();
            if (labelIds.Count > 0)
            {
                var validLabelCount = await _db.Labels
                    .Where(l => l.ProjectId == projectId && labelIds.Contains(l.LabelId))
                    .CountAsync();

                if (validLabelCount != labelIds.Count)
                    throw new InvalidOperationException("One or more labels are invalid for this project.");
            }

            // 4) Get or Create DRAFT submission (first save creates it)
            // IMPORTANT: luôn dùng draft (sentinel) để Save không bị chặn bởi submission thật cũ.
            var submission = await GetOrCreateDraftAsync(taskItemId, annotatorId);

            // Khóa edit khi đã vào review/final (Rejected cho phép rework qua draft mới)
            if (submission.Status == SubmissionStatus.InReview
                || submission.Status == SubmissionStatus.Approved)
            {
                throw new InvalidOperationException("This submission can no longer be edited.");
            }

            // 5) Replace annotations (simple & safe for now)
            var existing = await _db.Annotations
                .Where(a => a.DataItemSubmissionId == submission.DataItemSubmissionId)
                .ToListAsync();

            if (existing.Count > 0)
                _db.Annotations.RemoveRange(existing);

            if (boxes.Count > 0)
            {
                var newEntities = boxes.Select(b => new Annotation
                {
                    DataItemSubmissionId = submission.DataItemSubmissionId,
                    LabelId = b.LabelId,
                    X = b.X,
                    Y = b.Y,
                    Width = b.Width,
                    Height = b.Height
                });

                _db.Annotations.AddRange(newEntities);
            }

            await _db.SaveChangesAsync();

            return submission.DataItemSubmissionId;
        }

        public async Task<List<AnnotationBoxDto>> GetSavedBoxesAsync(int taskItemId, int annotatorId)
        {
            await EnsureAnnotatorOwnsTaskItemAsync(taskItemId, annotatorId);
            // Load ưu tiên DRAFT (nháp). Nếu không còn draft (đã Submit) thì fallback sang submission mới nhất
            // để tránh UX "Submit xong quay lại bị rỗng".
            var draft = await GetLatestDraftAsync(taskItemId, annotatorId);

            DataItemSubmission? submission = draft;
            if (submission == null)
            {
                submission = await _db.DataItemSubmissions
                    .Where(s => s.TaskItemId == taskItemId && s.SubmittedBy == annotatorId)
                    .OrderByDescending(s => s.DataItemSubmissionId)
                    .FirstOrDefaultAsync();
            }

            if (submission == null) return new List<AnnotationBoxDto>();

            var boxes = await _db.Annotations
                .Where(a => a.DataItemSubmissionId == submission.DataItemSubmissionId)
                .OrderBy(a => a.AnnotationId)
                .Select(a => new AnnotationBoxDto
                {
                    LabelId = a.LabelId,
                    X = a.X,
                    Y = a.Y,
                    Width = a.Width,
                    Height = a.Height
                })
                .ToListAsync();

            return boxes;
        }

        public async Task SubmitAsync(int taskItemId, int annotatorId)
        {
            await EnsureAnnotatorOwnsTaskItemAsync(taskItemId, annotatorId);
            // Submit chỉ chốt DRAFT hiện tại
            var submission = await GetLatestDraftAsync(taskItemId, annotatorId);

            if (submission == null)
                throw new Exception("There are no annotations have been saved yet.");

            // Rule tối thiểu: phải có ít nhất 1 bbox
            var hasAny = await _db.Annotations
                .AnyAsync(a => a.DataItemSubmissionId == submission.DataItemSubmissionId);

            if (!hasAny)
                throw new Exception("There are currently no bbox.");

            // Nếu đã Approved thì thường không cho submit lại (tuỳ bạn)
            if (submission.Status == SubmissionStatus.Approved)
                throw new Exception("This submission has been approved and cannot be submitted again.");

            // Draft luôn sentinel. Nếu (bất thường) draft không sentinel thì chặn.
            if (!IsSentinel(submission.SubmittedAt))
                throw new Exception("This submission has already been submitted.");

            submission.Status = SubmissionStatus.Submitted;
            submission.SubmittedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        public async Task SetInReviewAsync(int dataItemSubmissionId, int reviewerId)
        {
            var submission = await _db.DataItemSubmissions
                .Include(s => s.Review)
                .FirstOrDefaultAsync(s => s.DataItemSubmissionId == dataItemSubmissionId);

            if (submission == null) throw new InvalidOperationException("Submission not found.");

            // Must be submitted for real
            if (IsSentinel(submission.SubmittedAt))
                throw new InvalidOperationException("Submission has not been submitted yet.");

            // Only from Submitted -> InReview
            if (submission.Status != SubmissionStatus.Submitted)
                throw new InvalidOperationException("Only Submitted submissions can be moved to InReview.");

            // 1 submission should have max 1 review record
            if (submission.Review != null)
                throw new InvalidOperationException("Submission has already been reviewed.");

            submission.Status = SubmissionStatus.InReview;
            await _db.SaveChangesAsync();
        }

        public async Task ApproveAsync(int dataItemSubmissionId, int reviewerId, string? comment)
        {
            await DecideAsync(dataItemSubmissionId, reviewerId, ReviewDecision.Approved, comment);
        }

        public async Task RejectAsync(int dataItemSubmissionId, int reviewerId, string? comment)
        {
            await DecideAsync(dataItemSubmissionId, reviewerId, ReviewDecision.Rejected, comment);
        }

        // helper private method (đặt ngay dưới 2 method trên)
        private async Task DecideAsync(int submissionId, int reviewerId, ReviewDecision decision, string? comment)
        {
            var submission = await _db.DataItemSubmissions
                .Include(s => s.Review)
                .FirstOrDefaultAsync(s => s.DataItemSubmissionId == submissionId);

            if (submission == null) throw new InvalidOperationException("Submission not found.");

            if (submission.Status != SubmissionStatus.InReview)
                throw new InvalidOperationException("Only InReview submissions can be approved/rejected.");

            if (submission.Review != null)
                throw new InvalidOperationException("Submission has already been reviewed.");

            submission.Status = (decision == ReviewDecision.Approved)
                ? SubmissionStatus.Approved
                : SubmissionStatus.Rejected;

            // Insert review record (schema data_item_reviews)
            var review = new DataItemReview
            {
                DataItemSubmissionId = submission.DataItemSubmissionId,
                ReviewerId = reviewerId,
                Decision = decision,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                ReviewedAt = DateTime.UtcNow
            };

            _db.DataItemReviews.Add(review);

            await _db.SaveChangesAsync();

            // Completion Rule: nếu Approved thì check xem Task đã hoàn thành chưa
            if (decision == ReviewDecision.Approved)
            {
                await TryCompleteTaskAsync(submission.TaskItemId);
            }
        }

        /// <summary>
        /// Kiểm tra nếu tất cả TaskItem trong cùng Task đều có submission mới nhất Approved
        /// thì tự động set TaskEntity.Status = Completed.
        /// </summary>
        private async Task TryCompleteTaskAsync(int taskItemId)
        {
            var taskItem = await _db.TaskItems
                .Include(ti => ti.Task)
                    .ThenInclude(t => t.TaskItems)
                        .ThenInclude(ti => ti.Submissions)
                .FirstOrDefaultAsync(ti => ti.TaskItemId == taskItemId);

            if (taskItem == null) return;

            var task = taskItem.Task;

            var allApproved = task.TaskItems.All(ti =>
            {
                var latestReal = ti.Submissions
                    .Where(s => !IsSentinel(s.SubmittedAt))
                    .OrderByDescending(s => s.DataItemSubmissionId)
                    .FirstOrDefault();
                return latestReal?.Status == SubmissionStatus.Approved;
            });

            if (allApproved && task.Status != Enums.TaskStatus.Completed)
            {
                task.Status = Enums.TaskStatus.Completed;
                await _db.SaveChangesAsync();
            }
        }
        public async Task<ReviewSubmissionDetailDto> GetReviewSubmissionDetailAsync(int dataItemSubmissionId, int reviewerId)
        {
            var submission = await _db.DataItemSubmissions
                .AsNoTracking()
                .Include(s => s.Submitter)
                .Include(s => s.TaskItem)
                    .ThenInclude(ti => ti.DataItem)
                .Include(s => s.Annotations)
                    .ThenInclude(a => a.Label)
                .FirstOrDefaultAsync(s => s.DataItemSubmissionId == dataItemSubmissionId);

            if (submission == null)
                throw new InvalidOperationException("Submission not found.");

            // chặn draft sentinel (Save != Submit)
            if (IsSentinel(submission.SubmittedAt))
                throw new InvalidOperationException("Submission has not been submitted yet.");

            var dto = new ReviewSubmissionDetailDto
            {
                SubmissionId = submission.DataItemSubmissionId,
                TaskItemId = submission.TaskItemId,
                SubmittedBy = submission.SubmittedBy,
                SubmittedByName = submission.Submitter.Name ?? submission.Submitter.Username,
                SubmittedAtUtc = submission.SubmittedAt,
                Status = submission.Status.ToString(),
                ImagePath = submission.TaskItem.DataItem.ImagePath,
                Boxes = submission.Annotations
                    .OrderBy(a => a.AnnotationId)
                    .Select(a => new AnnotationBoxDto
                    {
                        LabelId = a.LabelId,
                        LabelName = a.Label.Name,
                        X = a.X,
                        Y = a.Y,
                        Width = a.Width,
                        Height = a.Height
                    })
                    .ToList()
            };

            // (để vẽ đúng màu label)
            dto.LabelColors = submission.Annotations
                             .GroupBy(a => a.LabelId)
                             .ToDictionary(g => g.Key, g => g.First().Label.Color);

            return dto;
        }
        public async Task<List<ReviewQueueItemDto>> GetReviewQueueAsync(int reviewerId)
        {
            // reviewerId chưa dùng ở rule hiện tại, nhưng để sẵn cho phân quyền/assign sau này

            var sentinel = SentinelSubmittedAt;

            var query = _db.DataItemSubmissions
                .AsNoTracking()
                .Where(s => s.SubmittedAt != sentinel);

            // Sort: Submitted → InReview → Approved → Rejected; trong mỗi nhóm thì SubmittedAt mới nhất trước
            query = query
                .OrderBy(s => s.Status == SubmissionStatus.Submitted ? 0 :
                              s.Status == SubmissionStatus.InReview ? 1 :
                              s.Status == SubmissionStatus.Approved ? 2 : 3)
                .ThenByDescending(s => s.SubmittedAt);

            return await query
                .Select(s => new ReviewQueueItemDto
                {
                    SubmissionId = s.DataItemSubmissionId,
                    TaskItemId = s.TaskItemId,
                    SubmittedBy = s.SubmittedBy,
                    SubmittedByName = s.Submitter.Name ?? s.Submitter.Username,
                    SubmittedAtUtc = s.SubmittedAt,
                    Status = s.Status.ToString()
                })
                .ToListAsync();
        }
    }
}