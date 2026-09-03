using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using QuickStockTaker.Models;
using QuickStockTaker.Core.Services.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    #region Fields
    private List<DonationAmount> _donationAmounts;
    private readonly IPageDialogService _pageDialogService;
    private readonly IEntitlementService _entitlementService;
    #endregion

    [ObservableProperty]
    private string _versionNo;

    [ObservableProperty]
    private string _amount;

    [ObservableProperty]
    private bool _isPro;

    [ObservableProperty]
    private bool _showUpgradeToPro;

    [ObservableProperty]
    private bool _isPurchaseInProgress;

    public AboutViewModel(
        IPageDialogService pageDialogService,
        IEntitlementService entitlementService)
    {
        _pageDialogService = pageDialogService;
        _entitlementService = entitlementService;
        _donationAmounts = new List<DonationAmount>()
        {
            new DonationAmount() { Amount = (decimal)0.99, AmountString = "$0.99" },
            new DonationAmount() { Amount = (decimal)2.99, AmountString = "$2.99" },
            new DonationAmount() { Amount = (decimal)4.99, AmountString = "$4.99" }
        };

        Log.Information("Start AboutViewModel");
    }

    [RelayCommand]
    private async Task OnDonation()
    {
        var amounts = _donationAmounts.Select(x => x.AmountString).ToArray();

        var action = await _pageDialogService.DisplayActionSheetAsync("Select a amount you would like to donate", "Cancel", null, amounts);
        //DoDonation();
    }

    [RelayCommand]
    private async Task OnProjectHome(string url)
    {
        await Launcher.Default.OpenAsync(url);
        //DoDonation();
    }

    /// <summary>
    /// Take the donation amount
    /// </summary>
    /// <param name="amount">donation amount</param>
    /// <returns></returns>
    private void DoDonation(decimal amount)
    {
        new NotImplementedException();
    }

    /// <summary>
    /// Handle Appearing event
    /// </summary>
    [RelayCommand]
    private async Task OnAppearing()
    {
        VersionNo = AppInfo.Current.VersionString;
        await _entitlementService.InitializeAsync();
        await _entitlementService.RefreshAsync();
        UpdateEntitlementState();
    }

    [RelayCommand]
    private async Task OnUpgradeToPro()
    {
        IsPurchaseInProgress = true;
        try
        {
            var result = await _entitlementService.PurchaseProAsync();
            switch (result.Status)
            {
                case ProPurchaseStatus.Purchased:
                case ProPurchaseStatus.AlreadyOwned:
                    UpdateEntitlementState();
                    await _pageDialogService.DisplayAlertAsync(
                        "Pro unlocked",
                        "Quick Stocktaker Pro is active with unlimited items, Saved Stocktakes, and export sharing.",
                        "OK");
                    break;

                case ProPurchaseStatus.Pending:
                    await _pageDialogService.DisplayAlertAsync(
                        "Purchase pending",
                        "Google Play is still processing the purchase. Pro will unlock after payment completes.",
                        "OK");
                    break;

                case ProPurchaseStatus.Cancelled:
                    break;

                case ProPurchaseStatus.Unavailable:
                case ProPurchaseStatus.Failed:
                    await _pageDialogService.DisplayAlertAsync(
                        "Purchase unsuccessful",
                        result.ErrorMessage ?? "Google Play could not complete the purchase.",
                        "OK");
                    break;
            }
        }
        finally
        {
            IsPurchaseInProgress = false;
        }
    }

    private void UpdateEntitlementState()
    {
        IsPro = _entitlementService.IsPro;
        ShowUpgradeToPro = _entitlementService.IsPurchaseSupported && !IsPro;
    }
}

