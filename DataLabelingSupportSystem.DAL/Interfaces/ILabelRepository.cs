using DataLabelingSupportSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Interfaces
{
    public interface ILabelRepository
    {
        Task <List<Label>> GetByProjectIdAsync(int ProjectId);
        Task AddAsync(Label label);
        Task DeleteAsync(int labelId);
        Task <Label> GetByIdAsync(int labelId);
        Task UpdateAsync(Label label);

    }
}
