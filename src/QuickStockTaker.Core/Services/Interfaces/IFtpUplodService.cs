namespace QuickStockTaker.Core.Services.Interfaces
{
    /// <summary>
    /// intermediate interface for uploading file to ftp
    /// </summary>
    public interface IFtpUplodService : IUploader
    {
        Task<(bool, string)> ValidateSettings();
        Task<(bool, string)> TestConnection(CancellationToken cancellationToken = default);
        Task<(bool, string)> Upload(FileInfo file, CancellationToken cancellationToken);
    }
}
