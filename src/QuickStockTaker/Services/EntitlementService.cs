using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Services;

public sealed class EntitlementService : IEntitlementService
{
    private static readonly TimeSpan MinimumRefreshInterval = TimeSpan.FromSeconds(5);

    private readonly ISecureStorageService _secureStorageService;
    private readonly IPlatformPurchaseService _platformPurchaseService;
    private readonly ILogger<EntitlementService> _logger;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly object _initializationLock = new();

    private Task _initializationTask;
    private bool _isPro;
    private DateTimeOffset _lastSuccessfulRefresh = DateTimeOffset.MinValue;

    public EntitlementService(
        ISecureStorageService secureStorageService,
        IPlatformPurchaseService platformPurchaseService,
        ILogger<EntitlementService> logger)
    {
        _secureStorageService = secureStorageService;
        _platformPurchaseService = platformPurchaseService;
        _logger = logger;

        _platformPurchaseService.OwnershipChanged += OnOwnershipChanged;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool IsPro => _isPro;

    public bool IsPurchaseSupported => _platformPurchaseService.IsSupported;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        lock (_initializationLock)
        {
            _initializationTask ??= LoadCachedEntitlementAsync();
            return _initializationTask.WaitAsync(cancellationToken);
        }
    }

    public async Task RefreshAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        if (!IsPurchaseSupported ||
            (!force && DateTimeOffset.UtcNow - _lastSuccessfulRefresh < MinimumRefreshInterval))
        {
            return;
        }

        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            if (!force &&
                DateTimeOffset.UtcNow - _lastSuccessfulRefresh < MinimumRefreshInterval)
                return;

            var ownership = await _platformPurchaseService.GetOwnershipAsync(cancellationToken);
            switch (ownership)
            {
                case StoreOwnershipStatus.Owned:
                    await SaveEntitlementAsync(true);
                    _lastSuccessfulRefresh = DateTimeOffset.UtcNow;
                    break;

                case StoreOwnershipStatus.NotOwned:
                    await SaveEntitlementAsync(false);
                    _lastSuccessfulRefresh = DateTimeOffset.UtcNow;
                    break;

                case StoreOwnershipStatus.Unknown:
                    break;
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to refresh the Pro entitlement.");
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<ProPurchaseResult> PurchaseProAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsPurchaseSupported)
            return new ProPurchaseResult(ProPurchaseStatus.Unavailable);

        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            var result = await _platformPurchaseService.PurchaseProAsync(cancellationToken);
            if (result.Status is ProPurchaseStatus.Purchased or ProPurchaseStatus.AlreadyOwned)
                await SaveEntitlementAsync(true);

            return result;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to purchase the Pro entitlement.");
            return new ProPurchaseResult(ProPurchaseStatus.Failed, exception.Message);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private async Task LoadCachedEntitlementAsync()
    {
        try
        {
            var cachedValue = await _secureStorageService.GetAsync(Constants.ProEntitlement);
            _isPro = bool.TryParse(cachedValue, out var isPro) && isPro;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load the cached Pro entitlement.");
            _isPro = false;
        }
    }

    private async Task SaveEntitlementAsync(bool isPro)
    {
        _isPro = isPro;

        try
        {
            await _secureStorageService.SetAsync(Constants.ProEntitlement, isPro.ToString());
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to cache the Pro entitlement.");
        }
    }

    private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs eventArgs)
    {
        if (eventArgs.NetworkAccess == NetworkAccess.Internet)
            _ = RefreshAsync(force: true);
    }

    private void OnOwnershipChanged(object sender, EventArgs eventArgs) =>
        _ = RefreshAsync(force: true);
}
