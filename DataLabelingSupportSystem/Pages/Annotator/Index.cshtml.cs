using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.UI.Pages.Annotator
{
    [Authorize(Roles = "Annotator")]
    public class IndexModel : PageModel
    {
        private readonly ITaskService _taskService;

        public IndexModel(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public List<TaskViewDto> AssignedTasks { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                AssignedTasks = await _taskService.GetTasksByAnnotatorIdAsync(userId);
            }
        }
    }
}
