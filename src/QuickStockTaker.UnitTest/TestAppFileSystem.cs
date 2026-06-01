using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.UnitTest;

internal sealed class TestAppFileSystem : IAppFileSystem
{
    public TestAppFileSystem(string appDataDirectory)
    {
        AppDataDirectory = appDataDirectory;
    }

    public string AppDataDirectory { get; }

    public string GetDownloadFilePath(string fileName) => Path.Combine(AppDataDirectory, fileName);
}
