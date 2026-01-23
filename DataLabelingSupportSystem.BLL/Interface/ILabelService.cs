using DataLabelingSupportSystem.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.Interface
{
    public interface ILabelService
    {
        Task <List<LabelViewDto>> GetLabelsByProjectIdAsync(int projectId);
        Task AddLabelAsync (CreateLabelDto dto,int ProjectId);
        Task DeleteLabelAsync (int labelId);
        Task UpdateLabelAsync(int labelId, CreateLabelDto labelDto);
    }
}
