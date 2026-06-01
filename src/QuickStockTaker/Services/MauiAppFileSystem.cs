using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Services;

public class MauiAppFileSystem : IAppFileSystem
{
    public string AppDataDirectory => FileSystem.Current.AppDataDirectory;

    public string GetDownloadFilePath(string fileName)
    {
        var targetDir = string.Empty;
#if ANDROID
        targetDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
#elif IOS
        targetDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif
        return Path.Combine(targetDir, fileName);
    }
}
