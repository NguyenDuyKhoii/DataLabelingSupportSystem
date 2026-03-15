using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;

namespace DataLabelingSupportSystem.UI.Pages.Annotator
{
    [Authorize(Roles = "Annotator")]
    public class AnnotateModel : PageModel
    {
        private readonly IAnnotationService _annotationService;

        public AnnotateModel(IAnnotationService annotationService)
        {
            _annotationService = annotationService;
        }

        // Single property for UI to read
        public AnnotateContextDto Vm { get; private set; } = null!;

        [BindProperty]
        public int TaskItemId { get; set; }

        [BindProperty]
        public string BoxesJson { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int taskItemId)
        {
            var annotatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var vm = await _annotationService.GetAnnotateContextAsync(taskItemId, annotatorId);
            if (vm == null) return NotFound();

            Vm = vm;
            TaskItemId = taskItemId;

            return Page();
        }

        // ✅ NEW: GET /Annotator/Annotate/{taskItemId}?handler=Boxes
        // Return list of saved bboxes for UI to re-render on reload
        public async Task<IActionResult> OnGetBoxesAsync(int taskItemId)
        {
            var annotatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var boxes = await _annotationService.GetSavedBoxesAsync(taskItemId, annotatorId);
            return new JsonResult(new { ok = true, boxes });
        }

        // POST: /Annotator/Annotate/{taskItemId}?handler=Save
        // Body: BoxesJson = [ { labelId, x, y, width, height }, ... ]
        public async Task<IActionResult> OnPostSaveAsync()
        {
            var annotatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var boxes = new List<AnnotationBoxDto>();

                if (!string.IsNullOrWhiteSpace(BoxesJson))
                {
                    boxes = JsonSerializer.Deserialize<List<AnnotationBoxDto>>(
                                BoxesJson,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                           ) ?? new List<AnnotationBoxDto>();
                }

                var submissionId = await _annotationService.SaveAnnotationsAsync(TaskItemId, annotatorId, boxes);
                return new JsonResult(new { ok = true, submissionId });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, error = ex.Message }) { StatusCode = 400 };
            }
        }


        public async Task<IActionResult> OnPostSubmitAsync()
        {
            var annotatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                await _annotationService.SubmitAsync(TaskItemId, annotatorId);
                var nextId = await _annotationService.GetNextTaskItemIdAsync(TaskItemId, annotatorId);

                return new JsonResult(new { ok = true, nextTaskItemId = nextId });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, error = ex.Message }) { StatusCode = 400 };
            }
        }
    }
}