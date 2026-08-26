using FluentFTP;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Services
{
    internal sealed class FtpStocktakeRemoteTransferAdapter : IStocktakeRemoteTransferAdapter
    {
        private readonly Func<FileInfo, StocktakeRemoteConfiguration, string, CancellationToken, Task> _upload;

        public FtpStocktakeRemoteTransferAdapter()
            : this(UploadAsync)
        {
        }

        internal FtpStocktakeRemoteTransferAdapter(
            Func<FileInfo, StocktakeRemoteConfiguration, string, CancellationToken, Task> upload)
        {
            _upload = upload;
        }

        public StocktakeRemoteProtocol Protocol => StocktakeRemoteProtocol.Ftp;

        public Task TransferAsync(
            StocktakeExport export,
            StocktakeRemoteConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            var remotePath = FtpUplodService.BuildRemotePath(
                configuration.Folder,
                export.File.Name,
                path => path);

            return _upload(export.File, configuration, remotePath, cancellationToken);
        }

        private static async Task UploadAsync(
            FileInfo file,
            StocktakeRemoteConfiguration configuration,
            string remotePath,
            CancellationToken cancellationToken)
        {
            using var client = new AsyncFtpClient(
                configuration.Host,
                configuration.Username,
                configuration.Password,
                configuration.Port);

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
                throw new InvalidOperationException($"FTP upload returned status: {status}.");
        }
    }
}
