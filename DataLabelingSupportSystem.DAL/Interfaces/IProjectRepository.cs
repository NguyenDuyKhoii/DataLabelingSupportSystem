using DataLabelingSupportSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Interfaces
{
    public interface IProjectRepository
    {
        Task AddAsync(Project project);
        Task<Project?> GetByIdAsync(int projectId);
        Task <List<Project>> GetByManagerIdAsync(int managerId);
        Task UpdateAsync(Project project);
    }
}
