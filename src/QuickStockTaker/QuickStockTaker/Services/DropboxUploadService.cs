using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.Services.Interfaces;

namespace QuickStockTaker.Services
{
    /// <summary>
    /// Uploade data to dropbox
    /// </summary>
    class DropboxUploadService:IUploader
    {
        public string Name { get; set; }

        public Task<(bool, string)> Upload(FileInfo file)
        {
            throw new NotImplementedException();
        }
    }
}
