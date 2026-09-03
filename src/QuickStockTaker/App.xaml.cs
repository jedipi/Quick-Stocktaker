using Microsoft.Extensions.Logging;

namespace QuickStockTaker;

public partial class App : Application
{
    private readonly QuickStockTaker.Core.Services.Interfaces.IEntitlementService _entitlementService;
    private readonly ILogger<App> _logger;

    public App(
        QuickStockTaker.Core.Services.Interfaces.IEntitlementService entitlementService,
        ILogger<App> logger)
    {
        InitializeComponent();
        _entitlementService = entitlementService;
        _logger = logger;
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = new Window(new AppShell());
        window.Activated += (_, _) => _ = RefreshEntitlementAsync(force: false);
        window.Resumed += (_, _) => _ = RefreshEntitlementAsync(force: true);

        return window;
    }

    private async Task RefreshEntitlementAsync(bool force)
    {
        try
        {
            await _entitlementService.RefreshAsync(force);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to refresh the Pro entitlement during a lifecycle event.");
        }
    }
}
