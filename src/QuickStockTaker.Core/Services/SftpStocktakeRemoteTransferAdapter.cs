using QuickStockTaker.Core.Services.Interfaces;
using Renci.SshNet;

namespace QuickStockTaker.Core.Services
{
    internal sealed class SftpStocktakeRemoteTransferAdapter : IStocktakeRemoteTransferAdapter
    {
        private readonly Action<FileInfo, StocktakeRemoteConfiguration, string, CancellationToken> _upload;

        public SftpStocktakeRemoteTransferAdapter()
            : this(Upload)
        {
        }

        internal SftpStocktakeRemoteTransferAdapter(
            Action<FileInfo, StocktakeRemoteConfiguration, string, CancellationToken> upload)
        {
            _upload = upload;
        }

        public StocktakeRemoteProtocol Protocol => StocktakeRemoteProtocol.Sftp;

        public async Task TransferAsync(
            StocktakeExport export,
            StocktakeRemoteConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            var remotePath = FtpUplodService.BuildRemotePath(
                configuration.Folder,
                export.File.Name,
                path => path);

            // SSH.NET exposes synchronous transfer operations here; run them off the UI thread.
            await Task.Run(
                () => _upload(export.File, configuration, remotePath, cancellationToken),
                cancellationToken);
        }

        private static void Upload(
            FileInfo file,
            StocktakeRemoteConfiguration configuration,
            string remotePath,
            CancellationToken cancellationToken)
        {
            using var client = new SftpClient(
                configuration.Host,
                configuration.Port,
                configuration.Username,
                configuration.Password);
            using var cancellationRegistration = cancellationToken.Register(client.Dispose);
            cancellationToken.ThrowIfCancellationRequested();
            client.Connect();
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var directory in FtpUplodService.BuildSftpDirectoryPaths(remotePath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!client.Exists(directory))
                    client.CreateDirectory(directory);
            }

            using var stream = file.OpenRead();
            client.UploadFile(stream, remotePath, true);
            if (client.IsConnected)
                client.Disconnect();
        }
    }
}
