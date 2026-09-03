using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Services;

public sealed class UnsupportedPurchaseService : IPlatformPurchaseService
{
    public event EventHandler OwnershipChanged
    {
        add { }
        remove { }
    }

    public bool IsSupported => false;

    public Task<StoreOwnershipStatus> GetOwnershipAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(StoreOwnershipStatus.Unknown);

    public Task<ProPurchaseResult> PurchaseProAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProPurchaseResult(ProPurchaseStatus.Unavailable));
}
