using Android.BillingClient.Api;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services.Interfaces;
using BillingResponseCode = Android.BillingClient.Api.BillingResponseCode;

namespace QuickStockTaker.Services;

public sealed class GooglePlayPurchaseService : IPlatformPurchaseService
{
    private const int AcknowledgementAttemptCount = 3;
    private static readonly TimeSpan AcknowledgementRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PurchaseCallbackTimeout = TimeSpan.FromMinutes(10);

    private readonly ILogger<GooglePlayPurchaseService> _logger;
    private readonly object _connectionLock = new();

    private BillingClient _billingClient;
    private TaskCompletionSource<bool> _connectionCompletion;
    private PurchaseFlowState _activePurchaseFlow;
    private long _billingClientGeneration;

    public GooglePlayPurchaseService(ILogger<GooglePlayPurchaseService> logger)
    {
        _logger = logger;
    }

    public event EventHandler OwnershipChanged;

    public bool IsSupported => true;

    public async Task<StoreOwnershipStatus> GetOwnershipAsync(
        CancellationToken cancellationToken = default)
    {
        if (!await EnsureConnectedAsync(cancellationToken))
            return StoreOwnershipStatus.Unknown;

        var query = QueryPurchasesParams.NewBuilder()
            .SetProductType(BillingClient.ProductType.Inapp)
            .Build();
        var result = await _billingClient!.QueryPurchasesAsync(query);

        if (result.Result.ResponseCode != BillingResponseCode.Ok)
            return StoreOwnershipStatus.Unknown;

        var purchase = result.Purchases.FirstOrDefault(IsProPurchase);
        if (purchase is null)
            return StoreOwnershipStatus.NotOwned;

        if (purchase.PurchaseState != PurchaseState.Purchased)
            return StoreOwnershipStatus.Unknown;

        return await AcknowledgeIfNeededAsync(purchase, cancellationToken)
            ? StoreOwnershipStatus.Owned
            : StoreOwnershipStatus.Unknown;
    }

    public async Task<ProPurchaseResult> PurchaseProAsync(
        CancellationToken cancellationToken = default)
    {
        var ownership = await GetOwnershipAsync(cancellationToken);
        if (ownership == StoreOwnershipStatus.Owned)
            return new ProPurchaseResult(ProPurchaseStatus.AlreadyOwned);

        var product = await GetProProductDetailsAsync(cancellationToken);
        if (product is null)
        {
            return new ProPurchaseResult(
                ProPurchaseStatus.Unavailable,
                "The Pro upgrade is not available from Google Play.");
        }

        var activity = Platform.CurrentActivity;
        if (activity is null)
        {
            return new ProPurchaseResult(
                ProPurchaseStatus.Failed,
                "The Google Play purchase screen could not be opened.");
        }

        var productParameters = BillingFlowParams.ProductDetailsParams.NewBuilder()
            .SetProductDetails(product)
            .Build();
        var flowParameters = BillingFlowParams.NewBuilder()
            .SetProductDetailsParamsList(new[] { productParameters })
            .Build();

        var purchaseFlow = new PurchaseFlowState(Volatile.Read(ref _billingClientGeneration));
        if (Interlocked.CompareExchange(
                ref _activePurchaseFlow,
                purchaseFlow,
                null) is not null)
        {
            return new ProPurchaseResult(
                ProPurchaseStatus.Failed,
                "Another Google Play purchase is already in progress.");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var launchResult = _billingClient!.LaunchBillingFlow(activity, flowParameters);
            if (launchResult.ResponseCode == BillingResponseCode.UserCancelled)
                return new ProPurchaseResult(ProPurchaseStatus.Cancelled);

            if (launchResult.ResponseCode == BillingResponseCode.ItemAlreadyOwned)
                return await GetAlreadyOwnedResultAsync(cancellationToken);

            if (launchResult.ResponseCode != BillingResponseCode.Ok)
            {
                return new ProPurchaseResult(
                    ProPurchaseStatus.Failed,
                    launchResult.DebugMessage);
            }

            (BillingResult Result, IList<Purchase> Purchases) update;
            try
            {
                update = await purchaseFlow.Completion.Task.WaitAsync(
                    PurchaseCallbackTimeout,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                ResetAbandonedPurchaseFlow(purchaseFlow);
                throw;
            }
            catch (TimeoutException)
            {
                ResetAbandonedPurchaseFlow(purchaseFlow);
                return new ProPurchaseResult(
                    ProPurchaseStatus.Failed,
                    "Google Play did not return the purchase result. Check your purchase history before trying again.");
            }

            if (update.Result.ResponseCode == BillingResponseCode.UserCancelled)
                return new ProPurchaseResult(ProPurchaseStatus.Cancelled);

            if (update.Result.ResponseCode == BillingResponseCode.ItemAlreadyOwned)
                return await GetAlreadyOwnedResultAsync(cancellationToken);

            if (update.Result.ResponseCode != BillingResponseCode.Ok)
            {
                return new ProPurchaseResult(
                    ProPurchaseStatus.Failed,
                    update.Result.DebugMessage);
            }

            var purchase = update.Purchases?.FirstOrDefault(IsProPurchase);
            if (purchase is null)
            {
                return new ProPurchaseResult(
                    ProPurchaseStatus.Failed,
                    "Google Play did not return the completed purchase.");
            }

            if (purchase.PurchaseState == PurchaseState.Pending)
                return new ProPurchaseResult(ProPurchaseStatus.Pending);

            if (purchase.PurchaseState != PurchaseState.Purchased)
                return new ProPurchaseResult(ProPurchaseStatus.Failed);

            return await AcknowledgeIfNeededAsync(purchase, cancellationToken)
                ? new ProPurchaseResult(ProPurchaseStatus.Purchased)
                : new ProPurchaseResult(ProPurchaseStatus.Pending);
        }
        finally
        {
            Interlocked.CompareExchange(ref _activePurchaseFlow, null, purchaseFlow);
        }
    }

    private async Task<ProductDetails> GetProProductDetailsAsync(
        CancellationToken cancellationToken)
    {
        if (!await EnsureConnectedAsync(cancellationToken))
            return null;

        var product = QueryProductDetailsParams.Product.NewBuilder()
            .SetProductType(BillingClient.ProductType.Inapp)
            .SetProductId(Constants.ProProductId)
            .Build();
        var query = QueryProductDetailsParams.NewBuilder()
            .SetProductList(new[] { product })
            .Build();
        var result = await _billingClient!.QueryProductDetailsAsync(query);

        return result.ProductDetailsList?.FirstOrDefault(
            details => details.ProductId == Constants.ProProductId);
    }

    private Task<bool> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_billingClient?.IsReady == true)
            return Task.FromResult(true);

        lock (_connectionLock)
        {
            if (_billingClient?.IsReady == true)
                return Task.FromResult(true);

            if (_connectionCompletion is { Task.IsCompleted: false })
                return _connectionCompletion.Task.WaitAsync(cancellationToken);

            _connectionCompletion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var pendingPurchases = PendingPurchasesParams.NewBuilder()
                .EnableOneTimeProducts()
                .Build();
            if (_billingClient is null)
            {
                var generation = Interlocked.Increment(ref _billingClientGeneration);
                var builder = BillingClient.NewBuilder(Android.App.Application.Context);
                builder.SetListener((result, purchases) =>
                    OnPurchasesUpdated(generation, result, purchases));
                builder.EnablePendingPurchases(pendingPurchases);
                builder.EnableAutoServiceReconnection();
                _billingClient = builder.Build();
            }
            var connectionGeneration = Volatile.Read(ref _billingClientGeneration);
            _billingClient.StartConnection(
                result => OnBillingSetupFinished(connectionGeneration, result),
                () => OnBillingServiceDisconnected(connectionGeneration));

            return _connectionCompletion.Task.WaitAsync(cancellationToken);
        }
    }

    private void OnBillingSetupFinished(long generation, BillingResult result)
    {
        if (generation != Volatile.Read(ref _billingClientGeneration))
            return;

        var isConnected = result.ResponseCode == BillingResponseCode.Ok;
        if (!isConnected)
        {
            _logger.LogWarning(
                "Google Play Billing connection failed: {Message}",
                result.DebugMessage);
        }

        _connectionCompletion?.TrySetResult(isConnected);
    }

    private void OnBillingServiceDisconnected(long generation)
    {
        if (generation != Volatile.Read(ref _billingClientGeneration))
            return;

        _connectionCompletion?.TrySetResult(false);
    }

    private void OnPurchasesUpdated(
        long generation,
        BillingResult result,
        IList<Purchase> purchases)
    {
        if (generation != Volatile.Read(ref _billingClientGeneration))
        {
            NotifyOwnershipChanged(result, purchases);
            return;
        }

        var purchaseFlow = Volatile.Read(ref _activePurchaseFlow);
        if (purchaseFlow?.Generation == generation &&
            purchaseFlow.Completion.TrySetResult((result, purchases)))
            return;

        NotifyOwnershipChanged(result, purchases);
    }

    private void ResetAbandonedPurchaseFlow(PurchaseFlowState purchaseFlow)
    {
        BillingClient abandonedClient;
        lock (_connectionLock)
        {
            if (Interlocked.CompareExchange(
                    ref _activePurchaseFlow,
                    null,
                    purchaseFlow) != purchaseFlow)
            {
                return;
            }

            Interlocked.Increment(ref _billingClientGeneration);
            abandonedClient = _billingClient;
            _billingClient = null;
            _connectionCompletion = null;
        }

        try
        {
            abandonedClient?.EndConnection();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to close an abandoned Google Play Billing connection.");
        }

        OwnershipChanged?.Invoke(this, EventArgs.Empty);
    }

    private void NotifyOwnershipChanged(BillingResult result, IList<Purchase> purchases)
    {
        if (result.ResponseCode == BillingResponseCode.Ok &&
            purchases?.Any(IsProPurchase) == true)
        {
            OwnershipChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task<ProPurchaseResult> GetAlreadyOwnedResultAsync(
        CancellationToken cancellationToken)
    {
        var ownership = await GetOwnershipAsync(cancellationToken);
        return ownership == StoreOwnershipStatus.Owned
            ? new ProPurchaseResult(ProPurchaseStatus.AlreadyOwned)
            : new ProPurchaseResult(ProPurchaseStatus.Pending);
    }

    private async Task<bool> AcknowledgeIfNeededAsync(
        Purchase purchase,
        CancellationToken cancellationToken)
    {
        if (purchase.IsAcknowledged)
            return true;

        for (var attempt = 1; attempt <= AcknowledgementAttemptCount; attempt++)
        {
            try
            {
                var parameters = AcknowledgePurchaseParams.NewBuilder()
                    .SetPurchaseToken(purchase.PurchaseToken)
                    .Build();
                var result = await _billingClient!.AcknowledgePurchaseAsync(parameters);
                if (result.ResponseCode == BillingResponseCode.Ok)
                    return true;

                _logger.LogWarning(
                    "Google Play purchase acknowledgement attempt {Attempt} failed: {Message}",
                    attempt,
                    result.DebugMessage);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Google Play purchase acknowledgement attempt {Attempt} failed.",
                    attempt);
            }

            if (attempt < AcknowledgementAttemptCount)
                await Task.Delay(AcknowledgementRetryDelay, cancellationToken);
        }

        _logger.LogWarning(
            "Google Play purchase remains unacknowledged and will be retried on the next ownership refresh.");
        return false;
    }

    private static bool IsProPurchase(Purchase purchase) =>
        purchase.Products?.Contains(Constants.ProProductId) == true;

    private sealed class PurchaseFlowState(long generation)
    {
        public long Generation { get; } = generation;

        public TaskCompletionSource<(BillingResult Result, IList<Purchase> Purchases)> Completion
        {
            get;
        } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    }
}
