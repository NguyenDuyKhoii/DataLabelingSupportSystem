using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLabelingSupportSystem.DAL.DbContext;
using DataLabelingSupportSystem.DAL.Interfaces;
using DataLabelingSupportSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DataLabelingSupportSystem.DAL.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskEntity> CreateTaskAsync(TaskEntity task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<TaskEntity?> GetTaskByIdAsync(int taskId)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Annotator)
                .Include(t => t.TaskItems)
                    .ThenInclude(ti => ti.DataItem)
                .FirstOrDefaultAsync(t => t.TaskId == taskId);
        }

        public async Task<List<TaskEntity>> GetTasksByAnnotatorIdAsync(int annotatorId)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskItems)
                .Where(t => t.AnnotatorId == annotatorId)
                .ToListAsync();
        }

        public async Task<List<TaskEntity>> GetTasksByProjectIdAsync(int projectId)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Annotator)
                .Include(t => t.TaskItems)
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();
        }
    }
}
