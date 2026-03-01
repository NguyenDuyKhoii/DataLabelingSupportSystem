using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.UI.Pages.Annotator
{
    [Authorize(Roles = "Annotator")]
    public class TaskDetailsModel : PageModel
    {
        private readonly ITaskService _taskService;

        public TaskDetailsModel(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public TaskDetailDto TaskDetail { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var task = await _taskService.GetTaskDetailsByIdAsync(id);
            if (task == null) return NotFound();

            // Verify that this task actually belongs to the current Annotator
            var assignedTasks = await _taskService.GetTasksByAnnotatorIdAsync(
                int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

            if (!assignedTasks.Exists(t => t.TaskId == id))
            {
                return Forbid(); // Or RedirectToPage("AccessDenied")
            }

            TaskDetail = task;
            return Page();
        }
    }
}
