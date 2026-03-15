using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.DAL.DbContext;
using DataLabelingSupportSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using static DataLabelingSupportSystem.DAL.Models.Enums;
using System.IO;

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

        // Sentinel Ä‘á»ƒ biá»ƒu diá»…n "Saved but not submitted" mÃ  khÃ´ng Ä‘á»•i schema
        // IMPORTANT: SQL Server/EF thÆ°á»ng tráº£ DateTime.Kind = Unspecified => khÃ´ng dÃ¹ng ToUniversalTime() Ä‘á»ƒ so sÃ¡nh sentinel.
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
        // Helpers: luÃ´n lÃ m viá»‡c vá»›i DRAFT (sentinel) Ä‘á»ƒ Save != Submit
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
            // Serializable Ä‘á»ƒ trÃ¡nh 2 request Ä‘á»“ng thá»i cÃ¹ng "khÃ´ng tháº¥y draft" rá»“i táº¡o 2 draft sentinel.
            await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var draft = await GetLatestDraftAsync(taskItemId, userId);
            if (draft != null)
            {
                await tx.CommitAsync();
                return draft;
            }

            // Rework: náº¿u submission má»›i nháº¥t bá»‹ Rejected â†’ táº¡o draft má»›i + copy annotations
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
                Status = SubmissionStatus.Submitted // draft phÃ¢n biá»‡t báº±ng sentinel
            };

            _db.DataItemSubmissions.Add(created);
            await _db.SaveChangesAsync();

            // Copy annotations tá»« submission bá»‹ reject Ä‘á»ƒ Annotator cÃ³ Ä‘iá»ƒm báº¯t Ä‘áº§u sá»­a
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
                        Height = a.Height,
                        IsOccluded = a.IsOccluded,
                        IsTruncated = a.IsTruncated
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
            // Context Æ°u tiÃªn "draft hiá»‡n táº¡i" (Saved-but-not-submitted).
            // Náº¿u khÃ´ng cÃ³ draft thÃ¬ fallback sang submission má»›i nháº¥t Ä‘á»ƒ UI khÃ´ng bá»‹ rá»—ng sau khi Submit.
            var draft = taskItem.Submissions
                .Where(s => s.SubmittedBy == annotatorId && s.SubmittedAt == SentinelSubmittedAt)
                .OrderByDescending(s => s.DataItemSubmissionId)
                .FirstOrDefault();

            var latest = taskItem.Submissions
                .Where(s => s.SubmittedBy == annotatorId)
                .OrderByDescending(s => s.DataItemSubmissionId)
                .FirstOrDefault();

            var display = draft ?? latest;

            // NOTE (flow chuáº©n theo schema + yÃªu cáº§u):
            // - KHÃ”NG táº¡o DataItemSubmission khi má»Ÿ trang (GET).
            // - Submission chá»‰ táº¡o khi Save/Submit láº§n Ä‘áº§u (POST).
            var labels = await _labelService.GetLabelsByProjectIdAsync(taskItem.Task.ProjectId);

            bool canEdit;
            if (display == null)
            {
                canEdit = true;
            }
            else
            {
                // Khi Ä‘Ã£ vÃ o review/final thÃ¬ khÃ³a edit Ä‘á»ƒ trÃ¡nh sá»­a ngÆ°á»£c luá»“ng
                // Rejected â†’ cho phÃ©p rework (canEdit = true)
                canEdit = display.Status != SubmissionStatus.InReview
                          && display.Status != SubmissionStatus.Approved;
            }

            // Load review comment náº¿u submission má»›i nháº¥t bá»‹ Rejected (hiá»ƒn thá»‹ lÃ½ do cho Annotator)
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

            // 1) Load TaskItem + ProjectId (Ä‘á»ƒ check Label thuá»™c project)
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
            // IMPORTANT: luÃ´n dÃ¹ng draft (sentinel) Ä‘á»ƒ Save khÃ´ng bá»‹ cháº·n bá»Ÿi submission tháº­t cÅ©.
            var submission = await GetOrCreateDraftAsync(taskItemId, annotatorId);

            // KhÃ³a edit khi Ä‘Ã£ vÃ o review/final (Rejected cho phÃ©p rework qua draft má»›i)
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
                    Height = b.Height,
                    IsOccluded = b.IsOccluded,
                    IsTruncated = b.IsTruncated
                });

                _db.Annotations.AddRange(newEntities);
            }

            await _db.SaveChangesAsync();

            return submission.DataItemSubmissionId;
        }

        public async Task<List<AnnotationBoxDto>> GetSavedBoxesAsync(int taskItemId, int annotatorId)
        {
            await EnsureAnnotatorOwnsTaskItemAsync(taskItemId, annotatorId);
            // Load Æ°u tiÃªn DRAFT (nhÃ¡p). Náº¿u khÃ´ng cÃ²n draft (Ä‘Ã£ Submit) thÃ¬ fallback sang submission má»›i nháº¥t
            // Ä‘á»ƒ trÃ¡nh UX "Submit xong quay láº¡i bá»‹ rá»—ng".
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
                    Height = a.Height,
                    IsOccluded = a.IsOccluded,
                    IsTruncated = a.IsTruncated
                })
                .ToListAsync();

            return boxes;
        }

        public async Task SubmitAsync(int taskItemId, int annotatorId)
        {
            await EnsureAnnotatorOwnsTaskItemAsync(taskItemId, annotatorId);
            // Submit chá»‰ chá»‘t DRAFT hiá»‡n táº¡i
            var submission = await GetLatestDraftAsync(taskItemId, annotatorId);

            if (submission == null)
                throw new Exception("There are no annotations have been saved yet.");

            // Rule tá»‘i thiá»ƒu: pháº£i cÃ³ Ã­t nháº¥t 1 bbox
            var hasAny = await _db.Annotations
                .AnyAsync(a => a.DataItemSubmissionId == submission.DataItemSubmissionId);

            if (!hasAny)
                throw new Exception("There are currently no bbox.");

            // Náº¿u Ä‘Ã£ Approved thÃ¬ thÆ°á»ng khÃ´ng cho submit láº¡i (tuá»³ báº¡n)
            if (submission.Status == SubmissionStatus.Approved)
                throw new Exception("This submission has been approved and cannot be submitted again.");

            // Draft luÃ´n sentinel. Náº¿u (báº¥t thÆ°á»ng) draft khÃ´ng sentinel thÃ¬ cháº·n.
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

        // helper private method (Ä‘áº·t ngay dÆ°á»›i 2 method trÃªn)
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

            // Completion Rule: náº¿u Approved thÃ¬ check xem Task Ä‘Ã£ hoÃ n thÃ nh chÆ°a
            if (decision == ReviewDecision.Approved)
            {
                await TryCompleteTaskAsync(submission.TaskItemId);
            }
        }

        /// <summary>
        /// Kiá»ƒm tra náº¿u táº¥t cáº£ TaskItem trong cÃ¹ng Task Ä‘á»u cÃ³ submission má»›i nháº¥t Approved
        /// thÃ¬ tá»± Ä‘á»™ng set TaskEntity.Status = Completed.
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

            // cháº·n draft sentinel (Save != Submit)
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
                        Height = a.Height,
                        IsOccluded = a.IsOccluded,
                        IsTruncated = a.IsTruncated
                    })
                    .ToList()
            };

            // (Ä‘á»ƒ váº½ Ä‘Ãºng mÃ u label)
            dto.LabelColors = submission.Annotations
                             .GroupBy(a => a.LabelId)
                             .ToDictionary(g => g.Key, g => g.First().Label.Color);

            return dto;
        }
        public async Task<List<ReviewQueueItemDto>> GetReviewQueueAsync(int reviewerId)
        {
            // reviewerId chÆ°a dÃ¹ng á»Ÿ rule hiá»‡n táº¡i, nhÆ°ng Ä‘á»ƒ sáºµn cho phÃ¢n quyá»n/assign sau nÃ y

            var sentinel = SentinelSubmittedAt;

            var query = _db.DataItemSubmissions
                .AsNoTracking()
                .Where(s => s.SubmittedAt != sentinel);

            // Sort: Submitted â†’ InReview â†’ Approved â†’ Rejected; trong má»—i nhÃ³m thÃ¬ SubmittedAt má»›i nháº¥t trÆ°á»›c
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

        public async Task<ProjectYoloExportDto?> GetProjectYoloExportAsync(int projectId)
        {
            var project = await _db.Projects
                .Include(p => p.Labels)
                .Include(p => p.DataItems)
                    .ThenInclude(di => di.TaskItems)
                        .ThenInclude(ti => ti.Submissions)
                            .ThenInclude(s => s.Annotations)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null) return null;

            var labelOrder = project.Labels.OrderBy(l => l.LabelId).ToList();
            var labelMap = labelOrder
                .Select((l, index) => new { l.LabelId, Index = index })
                .ToDictionary(x => x.LabelId, x => x.Index);

            var export = new ProjectYoloExportDto
            {
                ProjectName = project.Name,
                LabelNamesByOrder = labelOrder.Select(l => l.Name).ToList()
            };

            foreach (var di in project.DataItems)
            {
                var latestApproved = di.TaskItems
                    .SelectMany(ti => ti.Submissions)
                    .Where(s => s.Status == SubmissionStatus.Approved)
                    .OrderByDescending(s => s.DataItemSubmissionId)
                    .FirstOrDefault();

                if (latestApproved == null) continue;

                var item = new YoloItemDto
                {
                    FileName = Path.GetFileNameWithoutExtension(di.ImagePath) + ".txt",
                    ImagePath = di.ImagePath
                };

                foreach (var ann in latestApproved.Annotations)
                {
                    if (labelMap.TryGetValue(ann.LabelId, out int classIdx))
                    {
                        float xCenter = (float)(ann.X + ann.Width / 2);
                        float yCenter = (float)(ann.Y + ann.Height / 2);
                        item.YoloLines.Add($"{classIdx} {xCenter:0.000000} {yCenter:0.000000} {ann.Width:0.000000} {ann.Height:0.000000}");
                    }
                }

                if (item.YoloLines.Any())
                {
                    export.Items.Add(item);
                }
            }

            return export;
        }

        public async Task<int?> GetNextTaskItemIdAsync(int currentTaskItemId, int annotatorId)
        {
            var currentItem = await _db.TaskItems
                .Include(ti => ti.Task)
                .FirstOrDefaultAsync(ti => ti.TaskItemId == currentTaskItemId && ti.Task.AnnotatorId == annotatorId);

            if (currentItem == null) return null;

            var nextItem = await _db.TaskItems
                .Where(ti => ti.TaskId == currentItem.TaskId && ti.TaskItemId > currentTaskItemId)
                .OrderBy(ti => ti.TaskItemId)
                .Select(ti => new { 
                    ti.TaskItemId, 
                    IsApproved = ti.Submissions.Any(s => s.Status == SubmissionStatus.Approved) 
                })
                .FirstOrDefaultAsync(ti => !ti.IsApproved);

            return nextItem?.TaskItemId;
        }
        public async Task<int?> GetNextReviewSubmissionIdAsync(int currentSubmissionId)
        {
            var current = await _db.DataItemSubmissions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.DataItemSubmissionId == currentSubmissionId);
            if (current == null) return null;
            var next = await _db.DataItemSubmissions
                .AsNoTracking()
                .Where(s => s.DataItemSubmissionId > currentSubmissionId 
                            && (s.Status == SubmissionStatus.Submitted || s.Status == SubmissionStatus.InReview))
                .OrderBy(s => s.DataItemSubmissionId)
                .Select(s => (int?)s.DataItemSubmissionId)
                .FirstOrDefaultAsync();
            return next;
        }
    }
}
