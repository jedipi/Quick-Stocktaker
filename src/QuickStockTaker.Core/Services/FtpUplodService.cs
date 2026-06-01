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
        private readonly IAppPreferences _preferences;
        private readonly ISecureStorageService _secureStorage;

        public FtpUplodService(IAppPreferences preferences, ISecureStorageService secureStorage)
        {
            _preferences = preferences;
            _secureStorage = secureStorage;
        }

        public string Name { get; } = nameof(FtpUplodService);

        public async Task<(bool, string)> ValidateSettings()
        {
            var settings = await LoadSettings();
            var validationMessage = Validate(settings);

            return string.IsNullOrEmpty(validationMessage)
                ? (true, string.Empty)
                : (false, validationMessage);
        }

        public async Task<(bool, string)> TestConnection(CancellationToken cancellationToken = default)
        {
            var settings = await LoadSettings();
            var validationMessage = Validate(settings);
            if (!string.IsNullOrEmpty(validationMessage))
                return (false, validationMessage);

            try
            {
                if (settings.UseSftp)
                    await Task.Run(() => TestSftpConnection(settings, cancellationToken), cancellationToken);
                else
                    await TestFtpConnection(settings, cancellationToken);

                return (true, "Connection successful.");
            }
            catch (OperationCanceledException)
            {
                return (false, "Connection test cancelled.");
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    return (false, "Connection test cancelled.");

                return (false, $"Connection failed. {ex.Message}");
            }
        }

        public Task<(bool, string)> Upload(FileInfo file) => Upload(file, CancellationToken.None);

        public async Task<(bool, string)> Upload(FileInfo file, CancellationToken cancellationToken)
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
                    await Task.Run(() => UploadSftp(file, settings, cancellationToken), cancellationToken);
                else
                    await UploadFtp(file, settings, cancellationToken);

                return (true, $"Data uploaded successfully: {file.Name}");
            }
            catch (OperationCanceledException)
            {
                return (false, "Data upload cancelled.");
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    return (false, "Data upload cancelled.");

                return (false, $"Data upload failed. {ex.Message}");
            }
        }

        private async Task<FtpUploadSettings> LoadSettings()
        {
            var useSftp = _preferences.GetBool(Constants.FtpUseSftp, true);

            return new FtpUploadSettings(
                useSftp,
                _preferences.GetString(Constants.FtpHost, string.Empty),
                _preferences.GetString(Constants.FtpPort, useSftp ? "22" : "21"),
                _preferences.GetString(Constants.FtpFolder, string.Empty),
                await _secureStorage.GetAsync(Constants.FtpUsername) ?? string.Empty,
                await _secureStorage.GetAsync(Constants.FtpPassword) ?? string.Empty);
        }

        private static string Validate(FtpUploadSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Host))
                return "FTP/SFTP host is not configured or not valid.";

            if (!IsValidPort(settings.Port))
                return "FTP/SFTP port is not configured or not valid.";

            if (string.IsNullOrWhiteSpace(settings.Username))
                return "FTP/SFTP username is not configured.";

            if (string.IsNullOrWhiteSpace(settings.Password))
                return "FTP/SFTP password is not configured.";

            return string.Empty;
        }

        private static async Task TestFtpConnection(FtpUploadSettings settings, CancellationToken cancellationToken)
        {
            using var client = new AsyncFtpClient(
                settings.Host.Trim(),
                settings.Username,
                settings.Password,
                int.Parse(settings.Port));

            await client.Connect(cancellationToken);
            await client.Disconnect(cancellationToken);
        }

        private static async Task UploadFtp(FileInfo file, FtpUploadSettings settings, CancellationToken cancellationToken)
        {
            var remotePath = BuildRemotePath(settings.Folder, file.Name, path => path);

            using var client = new AsyncFtpClient(
                settings.Host.Trim(),
                settings.Username,
                settings.Password,
                int.Parse(settings.Port));

            await client.Connect(cancellationToken);
            var status = await client.UploadFile(
                file.FullName,
                remotePath,
                FtpRemoteExists.Overwrite,
                createRemoteDir: true,
                FtpVerify.None,
                progress: null,
                token: cancellationToken);
            await client.Disconnect(cancellationToken);

            if (status != FtpStatus.Success)
            {
                throw new InvalidOperationException($"FTP upload returned status: {status}.");
            }
        }

        private static void TestSftpConnection(FtpUploadSettings settings, CancellationToken cancellationToken)
        {
            using var client = new SftpClient(settings.Host.Trim(), int.Parse(settings.Port), settings.Username, settings.Password);
            using var cancellationRegistration = cancellationToken.Register(client.Dispose);
            cancellationToken.ThrowIfCancellationRequested();
            client.Connect();
            cancellationToken.ThrowIfCancellationRequested();
            client.Disconnect();
        }

        private static void UploadSftp(FileInfo file, FtpUploadSettings settings, CancellationToken cancellationToken)
        {
            var remotePath = BuildRemotePath(settings.Folder, file.Name, path => path);

            using var client = new SftpClient(settings.Host.Trim(), int.Parse(settings.Port), settings.Username, settings.Password);
            using var cancellationRegistration = cancellationToken.Register(client.Dispose);
            cancellationToken.ThrowIfCancellationRequested();
            client.Connect();
            cancellationToken.ThrowIfCancellationRequested();
            EnsureSftpDirectoryExists(client, remotePath, cancellationToken);

            using var stream = file.OpenRead();
            client.UploadFile(stream, remotePath, true);
            if (client.IsConnected)
                client.Disconnect();
        }

        private static void EnsureSftpDirectoryExists(SftpClient client, string remotePath, CancellationToken cancellationToken)
        {
            foreach (var directory in BuildSftpDirectoryPaths(remotePath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!client.Exists(directory))
                    client.CreateDirectory(directory);
            }
        }

        internal static bool IsValidPort(string port)
        {
            return int.TryParse(port, out var parsedPort) && parsedPort is >= 1 and <= 65535;
        }

        internal static IReadOnlyList<string> BuildSftpDirectoryPaths(string remotePath)
        {
            var directoryEnd = remotePath.LastIndexOf('/');
            if (directoryEnd <= 0 && !remotePath.StartsWith("/"))
                return [];

            var directory = directoryEnd == 0
                ? "/"
                : remotePath[..directoryEnd];

            var isRooted = directory.StartsWith("/");
            var parts = directory
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var paths = new List<string>();
            var current = isRooted ? "/" : string.Empty;
            foreach (var part in parts)
            {
                current = current == "/"
                    ? $"/{part}"
                    : string.IsNullOrEmpty(current)
                        ? part
                        : $"{current}/{part}";

                paths.Add(current);
            }

            return paths;
        }

        internal static string BuildRemotePath(string folder, string fileName, Func<string, string> encode)
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
