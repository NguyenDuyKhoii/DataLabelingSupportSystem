using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLabelingSupportSystem.DAL.Models;

namespace DataLabelingSupportSystem.DAL.Interfaces
{
    public interface ITaskRepository
    {
        Task<TaskEntity> CreateTaskAsync(TaskEntity task);
        Task<List<TaskEntity>> GetTasksByProjectIdAsync(int projectId);
        Task<List<TaskEntity>> GetTasksByAnnotatorIdAsync(int annotatorId);
        Task<TaskEntity?> GetTaskByIdAsync(int taskId);
    }
}
