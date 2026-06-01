namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface IAppFileSystem
    {
        string AppDataDirectory { get; }

        string GetDownloadFilePath(string fileName);
    }
}
