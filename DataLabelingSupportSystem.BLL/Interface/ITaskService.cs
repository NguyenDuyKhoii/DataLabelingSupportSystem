using System.Collections.Generic;
using System.Threading.Tasks;
using DataLabelingSupportSystem.BLL.DTO;

namespace DataLabelingSupportSystem.BLL.Interface
{
    public interface ITaskService
    {
        Task<TaskViewDto> CreateTaskAsync(CreateTaskDto dto);
        Task<List<TaskViewDto>> GetTasksByProjectIdAsync(int projectId);
        Task<List<TaskViewDto>> GetTasksByAnnotatorIdAsync(int annotatorId);
        Task<TaskDetailDto?> GetTaskDetailsByIdAsync(int taskId);
    }
}
