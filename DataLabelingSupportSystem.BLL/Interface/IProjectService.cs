using DataLabelingSupportSystem.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.Interface
{
    public interface IProjectService
    {
        Task CreateProjectAsync(CreateProjectDto dto ,int managerId);
        Task<List<ProjectViewDto>> GetProjectByManagerIdAsync(int managerId);
    }
}
