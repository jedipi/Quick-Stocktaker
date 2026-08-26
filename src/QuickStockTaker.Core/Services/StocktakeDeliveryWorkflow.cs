using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;

namespace QuickStockTaker.Core.Services
{
    public sealed class StocktakeDeliveryWorkflow : IStocktakeDeliveryWorkflow
    {
        private readonly ICsvExportService _csvExport;
        private readonly ILogger<StocktakeDeliveryWorkflow> _logger;
        private readonly StocktakeDeliveryOperationGate _operationGate;
        private readonly IAppPreferences _preferences;
        private readonly ISecureStorageService _secureStorage;
        private readonly StocktakeRemoteConfigurationValidator _remoteConfigurationValidator;
        private readonly IReadOnlyCollection<IStocktakeRemoteTransferAdapter> _remoteTransferAdapters;

        internal StocktakeDeliveryWorkflow(
            ICsvExportService csvExport,
            ILogger<StocktakeDeliveryWorkflow> logger)
            : this(csvExport, logger, new StocktakeDeliveryOperationGate())
        {
        }

        internal StocktakeDeliveryWorkflow(
            ICsvExportService csvExport,
            ILogger<StocktakeDeliveryWorkflow> logger,
            StocktakeDeliveryOperationGate operationGate)
        {
            _csvExport = csvExport;
            _logger = logger;
            _operationGate = operationGate;
        }

        internal StocktakeDeliveryWorkflow(
            ICsvExportService csvExport,
            ILogger<StocktakeDeliveryWorkflow> logger,
            StocktakeDeliveryOperationGate operationGate,
            IAppPreferences preferences,
            ISecureStorageService secureStorage,
            StocktakeRemoteConfigurationValidator remoteConfigurationValidator,
            IEnumerable<IStocktakeRemoteTransferAdapter> remoteTransferAdapters)
        {
            _csvExport = csvExport;
            _logger = logger;
            _operationGate = operationGate;
            _preferences = preferences;
            _secureStorage = secureStorage;
            _remoteConfigurationValidator = remoteConfigurationValidator;
            _remoteTransferAdapters = remoteTransferAdapters.ToArray();
        }

        public async Task<StocktakeDeliveryResult> CreateExportAsync(CancellationToken cancellationToken = default)
        {
            if (!_operationGate.TryEnter())
                return StocktakeDeliveryResult.AlreadyInProgress();

            try
            {
                var export = await _csvExport.CreateExportAsync(cancellationToken);
                return export is null
                    ? StocktakeDeliveryResult.NoStocktakeData()
                    : StocktakeDeliveryResult.Succeeded(export);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StocktakeDeliveryResult.Cancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stocktake export creation failed");
                return StocktakeDeliveryResult.Failed();
            }
            finally
            {
                _operationGate.Exit();
            }
        }

        public async Task<StocktakeDeliveryResult> DeliverToConfiguredRemoteAsync(
            CancellationToken cancellationToken = default,
            Action onTransferStarting = null)
        {
            if (!_operationGate.TryEnter())
                return StocktakeDeliveryResult.AlreadyInProgress();

            try
            {
                var configurationResult = await CaptureRemoteConfigurationAsync();
                if (configurationResult.ErrorMessage is not null)
                    return StocktakeDeliveryResult.InvalidConfiguration(configurationResult.ErrorMessage);

                var export = await _csvExport.CreateExportAsync(cancellationToken);
                if (export is null)
                    return StocktakeDeliveryResult.NoStocktakeData();

                onTransferStarting?.Invoke();
                var adapter = _remoteTransferAdapters.Single(
                    candidate => candidate.Protocol == configurationResult.Configuration.Protocol);
                await adapter.TransferAsync(export, configurationResult.Configuration, cancellationToken);

                return StocktakeDeliveryResult.Succeeded(
                    export,
                    $"Data uploaded successfully: {export.File.Name}");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StocktakeDeliveryResult.Cancelled();
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                return StocktakeDeliveryResult.Cancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stocktake remote delivery failed");
                return StocktakeDeliveryResult.Failed("Data upload failed. Please try again.");
            }
            finally
            {
                _operationGate.Exit();
            }
        }

        private async Task<(StocktakeRemoteConfiguration Configuration, string ErrorMessage)> CaptureRemoteConfigurationAsync()
        {
            var useSftp = _preferences.GetBool(Constants.FtpUseSftp, true);
            var input = new StocktakeRemoteConfigurationInput(
                useSftp,
                _preferences.GetString(Constants.FtpHost, string.Empty),
                _preferences.GetString(Constants.FtpPort, useSftp ? "22" : "21"),
                _preferences.GetString(Constants.FtpFolder, string.Empty),
                await _secureStorage.GetAsync(Constants.FtpUsername) ?? string.Empty,
                await _secureStorage.GetAsync(Constants.FtpPassword) ?? string.Empty);
            var validation = _remoteConfigurationValidator.Validate(input);
            if (!validation.IsValid)
                return (null, validation.Errors[0].ErrorMessage);

            return (new StocktakeRemoteConfiguration(
                input.UseSftp ? StocktakeRemoteProtocol.Sftp : StocktakeRemoteProtocol.Ftp,
                input.Host.Trim(),
                int.Parse(input.Port),
                input.Folder,
                input.Username,
                input.Password), null);
        }
    }
}
