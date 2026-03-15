using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace DataLabelingSupportSystem.UI.Pages.Reviewer
{
    [Authorize(Roles = "Reviewer,Manager")]
    public class ReviewModel : PageModel
    {
        private readonly IAnnotationService _annotationService;

        public ReviewSubmissionDetailDto? Detail { get; private set; }

        public ReviewModel(IAnnotationService annotationService)
        {
            _annotationService = annotationService;
        }

        private int GetUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out var id)) throw new InvalidOperationException("Invalid user id.");
            return id;
        }

        public async Task<IActionResult> OnGetAsync(int submissionId)
        {
            try
            {
                var reviewerId = GetUserId();
                Detail = await _annotationService.GetReviewSubmissionDetailAsync(submissionId, reviewerId);
                return Page();
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToPage("/Reviewer/Index");
            }
        }

        // POST: Submitted -> InReview
        public async Task<IActionResult> OnPostSetInReviewAsync(int submissionId)
        {
            try
            {
                var reviewerId = GetUserId();
                await _annotationService.SetInReviewAsync(submissionId, reviewerId);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ok = false, error = ex.Message });
            }
        }

        // POST: InReview -> Approved
        public async Task<IActionResult> OnPostApproveAsync(int submissionId, string? comment)
        {
            try
            {
                var reviewerId = GetUserId();
                await _annotationService.ApproveAsync(submissionId, reviewerId, comment ?? "");
                var nextId = await _annotationService.GetNextReviewSubmissionIdAsync(submissionId);
                return new JsonResult(new { ok = true, nextSubmissionId = nextId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ok = false, error = ex.Message });
            }
        }

        // POST: InReview -> Rejected
        public async Task<IActionResult> OnPostRejectAsync(int submissionId, string? comment)
        {
            try
            {
                var reviewerId = GetUserId();
                await _annotationService.RejectAsync(submissionId, reviewerId, comment ?? "");
                var nextId = await _annotationService.GetNextReviewSubmissionIdAsync(submissionId);
                return new JsonResult(new { ok = true, nextSubmissionId = nextId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { ok = false, error = ex.Message });
            }
        }
    }
}