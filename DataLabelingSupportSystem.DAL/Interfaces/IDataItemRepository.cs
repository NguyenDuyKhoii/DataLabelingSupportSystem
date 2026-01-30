using DataLabelingSupportSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Interfaces
{
    public interface IDataItemRepository
    {
        Task AddRangeAsync(List<DataItem> items); 
        Task<List<DataItem>> GetByProjectIdAsync(int projectId);
    }
}
