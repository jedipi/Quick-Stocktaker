namespace QuickStockTaker.Core.Services.Interfaces;

public interface IPlatformPurchaseService
{
    event EventHandler OwnershipChanged;

    bool IsSupported { get; }

    Task<StoreOwnershipStatus> GetOwnershipAsync(CancellationToken cancellationToken = default);

    Task<ProPurchaseResult> PurchaseProAsync(CancellationToken cancellationToken = default);
}

public enum StoreOwnershipStatus
{
    Owned,
    NotOwned,
    Unknown
}
