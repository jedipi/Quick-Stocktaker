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
        private readonly StocktakeEmailConfigurationValidator _emailConfigurationValidator;
        private readonly IStocktakeEmailAdapter _emailAdapter;
        private readonly IStocktakeEmailConfigurationGate _emailConfigurationGate;

        public event Action EmailDeliveryStarting;

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

        internal StocktakeDeliveryWorkflow(
            ICsvExportService csvExport,
            ILogger<StocktakeDeliveryWorkflow> logger,
            StocktakeDeliveryOperationGate operationGate,
            IAppPreferences preferences,
            ISecureStorageService secureStorage,
            StocktakeRemoteConfigurationValidator remoteConfigurationValidator,
            IEnumerable<IStocktakeRemoteTransferAdapter> remoteTransferAdapters,
            StocktakeEmailConfigurationValidator emailConfigurationValidator,
            IStocktakeEmailAdapter emailAdapter)
            : this(
                csvExport,
                logger,
                operationGate,
                preferences,
                secureStorage,
                remoteConfigurationValidator,
                remoteTransferAdapters,
                emailConfigurationValidator,
                emailAdapter,
                new StocktakeEmailConfigurationGate())
        {
        }

        internal StocktakeDeliveryWorkflow(
            ICsvExportService csvExport,
            ILogger<StocktakeDeliveryWorkflow> logger,
            StocktakeDeliveryOperationGate operationGate,
            IAppPreferences preferences,
            ISecureStorageService secureStorage,
            StocktakeRemoteConfigurationValidator remoteConfigurationValidator,
            IEnumerable<IStocktakeRemoteTransferAdapter> remoteTransferAdapters,
            StocktakeEmailConfigurationValidator emailConfigurationValidator,
            IStocktakeEmailAdapter emailAdapter,
            IStocktakeEmailConfigurationGate emailConfigurationGate)
            : this(
                csvExport,
                logger,
                operationGate,
                preferences,
                secureStorage,
                remoteConfigurationValidator,
                remoteTransferAdapters)
        {
            _emailConfigurationValidator = emailConfigurationValidator;
            _emailAdapter = emailAdapter;
            _emailConfigurationGate = emailConfigurationGate;
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

        public async Task<StocktakeDeliveryResult> DeliverByEmailAsync(
            string recipient,
            CancellationToken cancellationToken = default)
        {
            if (!_operationGate.TryEnter())
                return StocktakeDeliveryResult.AlreadyInProgress();

            try
            {
                var configurationResult = await CaptureEmailConfigurationAsync(recipient);
                if (configurationResult.ErrorMessage is not null)
                    return StocktakeDeliveryResult.InvalidConfiguration(configurationResult.ErrorMessage);

                var content = new StocktakeEmailContent(
                    _preferences.GetString(Constants.DeviceId, string.Empty),
                    _preferences.GetInt(Constants.StocktakeNumber, 0),
                    _preferences.GetString(Constants.Site, string.Empty),
                    _preferences.GetDateTime(Constants.StocktakeDate, DateTime.MinValue).ToShortDateString());
                var export = await _csvExport.CreateExportAsync(cancellationToken);
                if (export is null)
                    return StocktakeDeliveryResult.NoStocktakeData();

                var delivery = new StocktakeEmailDelivery(
                    export,
                    recipient,
                    configurationResult.Sender,
                    configurationResult.Configuration,
                    content);

                EmailDeliveryStarting?.Invoke();
                await _emailAdapter.SendAsync(delivery, cancellationToken);

                return StocktakeDeliveryResult.Succeeded(export, "Data send successfully.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StocktakeDeliveryResult.Cancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stocktake email delivery failed");
                return StocktakeDeliveryResult.Failed("Data send fail.");
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
                var configurationResult = await StocktakeRemoteConfigurationSnapshot.CaptureAsync(
                    _preferences,
                    _secureStorage,
                    _remoteConfigurationValidator);
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

        private async Task<(StocktakeEmailConfiguration Configuration, string Sender, string ErrorMessage)> CaptureEmailConfigurationAsync(
            string recipient)
        {
            return await _emailConfigurationGate.RunAsync<(
                StocktakeEmailConfiguration Configuration,
                string Sender,
                string ErrorMessage)>(async () =>
            {
                var provider = _preferences.GetString(Constants.SmtpProvider, "Other");
                var configuredSenderTask = _secureStorage.GetAsync(Constants.SmtpFrom);
                var hostTask = _secureStorage.GetAsync(Constants.SmtpHost);
                var portTask = _secureStorage.GetAsync(Constants.SmtpPort);
                var usernameTask = _secureStorage.GetAsync(Constants.SmtpUsername);
                var passwordTask = _secureStorage.GetAsync(Constants.SmtpPassword);
                await Task.WhenAll(
                    configuredSenderTask,
                    hostTask,
                    portTask,
                    usernameTask,
                    passwordTask);

                var configuredSender = await configuredSenderTask;
                var input = new StocktakeEmailConfigurationInput(
                    provider,
                    provider != "Other" ? recipient : configuredSender ?? string.Empty,
                    await hostTask ?? string.Empty,
                    await portTask ?? string.Empty,
                    await usernameTask ?? string.Empty,
                    await passwordTask ?? string.Empty);
                var validation = _emailConfigurationValidator.Validate(input);
                if (!validation.IsValid)
                    return (null, null, validation.Errors[0].ErrorMessage);

                return (new StocktakeEmailConfiguration(
                    input.Provider,
                    input.Host.Trim(),
                    int.Parse(input.Port),
                    input.Username,
                    input.Password), input.Sender, null);
            });
        }
    }
}
