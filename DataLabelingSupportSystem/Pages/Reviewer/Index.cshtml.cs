using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace DataLabelingSupportSystem.UI.Pages.Reviewer
{
    [Authorize(Roles = "Reviewer")]
    public class IndexModel : PageModel
    {
        private readonly IAnnotationService _annotationService;
        public List<ReviewQueueItemDto> Queue { get; set; } = new();
        public IndexModel(IAnnotationService annotationService)
        {
            _annotationService = annotationService;
        }

        public async Task OnGetAsync()
        {
            var reviewerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var reviewerId = int.Parse(reviewerIdStr!);

            Queue = await _annotationService.GetReviewQueueAsync(reviewerId);
        }
        public async Task<IActionResult> OnPostSetInReviewAsync(int submissionId)
        {
            var reviewerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                await _annotationService.SetInReviewAsync(submissionId, reviewerId);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, error = ex.Message }) { StatusCode = 400 };
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(int submissionId, string? comment)
        {
            var reviewerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                await _annotationService.ApproveAsync(submissionId, reviewerId, comment);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, error = ex.Message }) { StatusCode = 400 };
            }
        }

        public async Task<IActionResult> OnPostRejectAsync(int submissionId, string? comment)
        {
            var reviewerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                await _annotationService.RejectAsync(submissionId, reviewerId, comment);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, error = ex.Message }) { StatusCode = 400 };
            }
        }

        private int GetUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(idStr))
                throw new InvalidOperationException("UserId claim missing.");
            return int.Parse(idStr);
        }
    }
}
