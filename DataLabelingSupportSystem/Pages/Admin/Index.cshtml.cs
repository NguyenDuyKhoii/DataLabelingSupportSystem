using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.UI.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IAdminService _adminService;

        public SystemStatsDto Stats { get; private set; } = new();

        public IndexModel(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public async Task OnGetAsync()
        {
            Stats = await _adminService.GetSystemOverviewAsync();
        }
    }
}
