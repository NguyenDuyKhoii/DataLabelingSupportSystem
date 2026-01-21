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
        Task <List<Project>> GetByManagerIdAsync(int managerId);
    }
}
