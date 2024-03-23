using QuickStockTaker.Core.Models;

namespace QuickStockTaker.Core.Services.Interfaces
{
    /// <summary>
    /// intermediate interface for sending email
    /// </summary>
    public interface IEmailUploadService:IUploader
    {
        public string To { get; set; }
        public Smtp SmtpDetail { get; set; }
        public string From { get; set; }
    }
}
