using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.Interface
{
    public interface IDataItemService
    {
        Task UploadImagesAsync(List<IFormFile> files, int projectId);

        
        Task<List<string>> GetImagesByProjectIdAsync(int projectId);
    }
}
