using FluentFTP;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services.Interfaces;
using Renci.SshNet;

namespace QuickStockTaker.Core.Services
{
    /// <summary>
    /// Upload data file to FTP or SFTP server.
    /// </summary>
    public class FtpUplodService : IFtpUplodService
    {
        public string Name { get; } = nameof(FtpUplodService);

        public async Task<(bool, string)> ValidateSettings()
        {
            var settings = await LoadSettings();
            var validationMessage = Validate(settings);

            return string.IsNullOrEmpty(validationMessage)
                ? (true, string.Empty)
                : (false, validationMessage);
        }

        public async Task<(bool, string)> TestConnection()
        {
            var settings = await LoadSettings();
            var validationMessage = Validate(settings);
            if (!string.IsNullOrEmpty(validationMessage))
                return (false, validationMessage);

            try
            {
                if (settings.UseSftp)
                    await Task.Run(() => TestSftpConnection(settings));
                else
                    await TestFtpConnection(settings);

                return (true, "Connection successful.");
            }
            catch (Exception ex)
            {
                return (false, $"Connection failed. {ex.Message}");
            }
        }

        public async Task<(bool, string)> Upload(FileInfo file)
        {
            if (file == null || !file.Exists)
                return (false, "Data upload failed. Exported file was not found.");

            var settings = await LoadSettings();
            var validationMessage = Validate(settings);
            if (!string.IsNullOrEmpty(validationMessage))
                return (false, validationMessage);

            try
            {
                if (settings.UseSftp)
                    await Task.Run(() => UploadSftp(file, settings));
                else
                    await UploadFtp(file, settings);

                return (true, $"Data uploaded successfully: {file.Name}");
            }
            catch (Exception ex)
            {
                return (false, $"Data upload failed. {ex.Message}");
            }
        }

        private static async Task<FtpUploadSettings> LoadSettings()
        {
            var useSftp = Preferences.Get(Constants.FtpUseSftp, true);

            return new FtpUploadSettings(
                useSftp,
                Preferences.Get(Constants.FtpHost, string.Empty),
                Preferences.Get(Constants.FtpPort, useSftp ? "22" : "21"),
                Preferences.Get(Constants.FtpFolder, string.Empty),
                await SecureStorage.GetAsync(Constants.FtpUsername) ?? string.Empty,
                await SecureStorage.GetAsync(Constants.FtpPassword) ?? string.Empty);
        }

        private static string Validate(FtpUploadSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Host))
                return "FTP/SFTP host is not configured.";

            if (!int.TryParse(settings.Port, out var port) || port <= 0)
                return "FTP/SFTP port is not configured.";

            if (string.IsNullOrWhiteSpace(settings.Username))
                return "FTP/SFTP username is not configured.";

            if (string.IsNullOrWhiteSpace(settings.Password))
                return "FTP/SFTP password is not configured.";

            return string.Empty;
        }

        private static async Task TestFtpConnection(FtpUploadSettings settings)
        {
            using var client = new AsyncFtpClient(
                settings.Host.Trim(),
                settings.Username,
                settings.Password,
                int.Parse(settings.Port));

            await client.Connect(CancellationToken.None);
            await client.Disconnect(CancellationToken.None);
        }

        private static async Task UploadFtp(FileInfo file, FtpUploadSettings settings)
        {
            var remotePath = BuildRemotePath(settings.Folder, file.Name, path => path);

            using var client = new AsyncFtpClient(
                settings.Host.Trim(),
                settings.Username,
                settings.Password,
                int.Parse(settings.Port));

            await client.Connect(CancellationToken.None);
            var status = await client.UploadFile(
                file.FullName,
                remotePath,
                FtpRemoteExists.Overwrite,
                createRemoteDir: true,
                FtpVerify.None,
                progress: null,
                token: CancellationToken.None);
            await client.Disconnect(CancellationToken.None);

            if (status != FtpStatus.Success)
            {
                throw new InvalidOperationException($"FTP upload returned status: {status}.");
            }
        }

        private static void TestSftpConnection(FtpUploadSettings settings)
        {
            using var client = new SftpClient(settings.Host.Trim(), int.Parse(settings.Port), settings.Username, settings.Password);
            client.Connect();
            client.Disconnect();
        }

        private static void UploadSftp(FileInfo file, FtpUploadSettings settings)
        {
            var remotePath = BuildRemotePath(settings.Folder, file.Name, path => path);

            using var client = new SftpClient(settings.Host.Trim(), int.Parse(settings.Port), settings.Username, settings.Password);
            client.Connect();

            using var stream = file.OpenRead();
            client.UploadFile(stream, remotePath, true);
            client.Disconnect();
        }

        private static string BuildRemotePath(string folder, string fileName, Func<string, string> encode)
        {
            var isRooted = !string.IsNullOrWhiteSpace(folder) && folder.TrimStart().StartsWith("/");
            var parts = folder?
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(encode)
                .ToList() ?? new List<string>();

            parts.Add(encode(fileName));

            var path = string.Join("/", parts);
            return isRooted ? $"/{path}" : path;
        }

        private sealed record FtpUploadSettings(
            bool UseSftp,
            string Host,
            string Port,
            string Folder,
            string Username,
            string Password);
    }
}
