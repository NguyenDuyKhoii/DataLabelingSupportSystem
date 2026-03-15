using DataLabelingSupportSystem.BLL.DTO;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.Interface
{
    public interface IAdminService
    {
        Task<SystemStatsDto> GetSystemOverviewAsync();
    }
}
