using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataLabelingSupportSystem.UI.Pages.Manager
{
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
            var detail = await _taskService.GetTaskDetailsByIdAsync(id);
            if (detail == null)
            {
                return NotFound();
            }

            TaskDetail = detail;
            return Page();
        }
    }
}
