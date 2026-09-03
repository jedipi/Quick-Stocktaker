using FluentAssertions;
using NSubstitute;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.UnitTest;

public sealed class AboutViewModelTests
{
    [Fact]
    public async Task UpgradeToProCommand_WhenPurchaseSucceeds_ShowsProStatus()
    {
        var dialogs = Substitute.For<IPageDialogService>();
        var entitlementService = Substitute.For<IEntitlementService>();
        entitlementService.IsPurchaseSupported.Returns(true);
        entitlementService.PurchaseProAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                entitlementService.IsPro.Returns(true);
                return new ProPurchaseResult(ProPurchaseStatus.Purchased);
            });
        var viewModel = new AboutViewModel(dialogs, entitlementService);

        await viewModel.UpgradeToProCommand.ExecuteAsync(null);

        viewModel.IsPro.Should().BeTrue();
        viewModel.ShowUpgradeToPro.Should().BeFalse();
        await dialogs.Received(1).DisplayAlertAsync(
            "Pro unlocked",
            "Quick Stocktaker Pro is active with unlimited items, Saved Stocktakes, and export sharing.",
            "OK");
    }

    [Fact]
    public async Task UpgradeToProCommand_WhenStorePurchaseIsCancelled_DoesNotShowAnAlert()
    {
        var dialogs = Substitute.For<IPageDialogService>();
        var entitlementService = Substitute.For<IEntitlementService>();
        entitlementService.PurchaseProAsync(Arg.Any<CancellationToken>())
            .Returns(new ProPurchaseResult(ProPurchaseStatus.Cancelled));
        var viewModel = new AboutViewModel(dialogs, entitlementService);

        await viewModel.UpgradeToProCommand.ExecuteAsync(null);

        await entitlementService.Received(1).PurchaseProAsync(Arg.Any<CancellationToken>());
        await dialogs.DidNotReceive().DisplayAlertAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }
}
