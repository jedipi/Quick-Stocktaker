using QuickStockTaker.Models;

namespace QuickStockTaker.Services.Interfaces
{
    /// <summary>
    /// intermediate interface for sending email
    /// </summary>
    public interface IEmailUploader:IUploader
    {
        public string To { get; set; }
        public Smtp SmtpDetail { get; set; }
        public string From { get; set; }
    }
}
