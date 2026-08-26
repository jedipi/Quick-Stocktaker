using FluentFTP;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;
using Renci.SshNet;

namespace QuickStockTaker.Core.Services
{
    internal sealed class StocktakeRemoteConnectionService : IStocktakeRemoteConnectionService
    {
        private readonly IAppPreferences _preferences;
        private readonly ISecureStorageService _secureStorage;
        private readonly StocktakeRemoteConfigurationValidator _validator;
        private readonly Func<StocktakeRemoteConfiguration, CancellationToken, Task> _testFtpConnection;
        private readonly Func<StocktakeRemoteConfiguration, CancellationToken, Task> _testSftpConnection;

        public StocktakeRemoteConnectionService(
            IAppPreferences preferences,
            ISecureStorageService secureStorage,
            StocktakeRemoteConfigurationValidator validator)
            : this(
                preferences,
                secureStorage,
                validator,
                TestFtpConnectionAsync,
                TestSftpConnectionAsync)
        {
        }

        internal StocktakeRemoteConnectionService(
            IAppPreferences preferences,
            ISecureStorageService secureStorage,
            StocktakeRemoteConfigurationValidator validator,
            Func<StocktakeRemoteConfiguration, CancellationToken, Task> testFtpConnection,
            Func<StocktakeRemoteConfiguration, CancellationToken, Task> testSftpConnection)
        {
            _preferences = preferences;
            _secureStorage = secureStorage;
            _validator = validator;
            _testFtpConnection = testFtpConnection;
            _testSftpConnection = testSftpConnection;
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync(
            CancellationToken cancellationToken = default)
        {
            var snapshot = await StocktakeRemoteConfigurationSnapshot.CaptureAsync(
                _preferences,
                _secureStorage,
                _validator);
            if (snapshot.ErrorMessage is not null)
                return (false, snapshot.ErrorMessage);

            var configuration = snapshot.Configuration;

            try
            {
                if (configuration.Protocol == StocktakeRemoteProtocol.Sftp)
                    await Task.Run(() => _testSftpConnection(configuration, cancellationToken), cancellationToken);
                else
                    await _testFtpConnection(configuration, cancellationToken);

                return (true, "Connection successful.");
            }
            catch (OperationCanceledException)
            {
                return (false, "Connection test cancelled.");
            }
            catch (Exception ex)
            {
                return cancellationToken.IsCancellationRequested
                    ? (false, "Connection test cancelled.")
                    : (false, $"Connection failed. {ex.Message}");
            }
        }

        private static async Task TestFtpConnectionAsync(
            StocktakeRemoteConfiguration configuration,
            CancellationToken cancellationToken)
        {
            using var client = new AsyncFtpClient(
                configuration.Host,
                configuration.Username,
                configuration.Password,
                configuration.Port);

            await client.Connect(cancellationToken);
            await client.Disconnect(cancellationToken);
        }

        private static Task TestSftpConnectionAsync(
            StocktakeRemoteConfiguration configuration,
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
            client.Disconnect();

            return Task.CompletedTask;
        }
    }
}
