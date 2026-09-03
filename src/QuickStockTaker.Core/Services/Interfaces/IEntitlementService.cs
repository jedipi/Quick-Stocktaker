namespace QuickStockTaker.Core.Services.Interfaces;

public interface IEntitlementService
{
    bool IsPro { get; }

    bool IsPurchaseSupported { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(
        bool force = false,
        CancellationToken cancellationToken = default);

    Task<ProPurchaseResult> PurchaseProAsync(CancellationToken cancellationToken = default);
}

public enum ProPurchaseStatus
{
    Purchased,
    AlreadyOwned,
    Pending,
    Cancelled,
    Unavailable,
    Failed
}

public sealed record ProPurchaseResult(ProPurchaseStatus Status, string ErrorMessage = null);
