using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.Interfaces;

namespace QuickStockTaker.Services
{
    /// <summary>
    /// Upload data file via iCloud
    /// </summary>
    public class ICloudUploadService:IUploader
    {
        public string Name { get; }

        public Task<(bool, string)> Upload(FileInfo file)
        {
            throw new NotImplementedException();
        }
    }
}
