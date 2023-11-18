using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using QuickStockTaker.Models;
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
    #endregion

    [ObservableProperty]
    private string _versionNo;

    [ObservableProperty]
    private string _amount;

    private readonly IUserDialogs _userDialogs;

    public AboutViewModel(IUserDialogs userDialogs)
    {
        _userDialogs = userDialogs;
        
        _donationAmounts = new List<DonationAmount>()
        {
            new DonationAmount() { Amount = (decimal)0.99, AmountString = "$0.99" },
            new DonationAmount() { Amount = (decimal)2.99, AmountString = "$2.99" },
            new DonationAmount() { Amount = (decimal)4.99, AmountString = "$4.99" }
        };

        Log.Information("Start AboutViewModel");
    }

    [RelayCommand]
    private void OnDonation()
    {
        var cfg = new ActionSheetConfig()
            .SetTitle("Select a amount you would like to donate")
            .SetMessage("Select a amount you would like to donate")
            .SetUseBottomSheet(false)
            .SetCancel();

        foreach (var donation in _donationAmounts)
        {
            cfg.Add(donation.AmountString, async () => await DoDonation(donation.Amount));
        }

        _userDialogs.ActionSheet(cfg);
    }

    /// <summary>
    /// Take the donation amount
    /// </summary>
    /// <param name="amount">donation amount</param>
    /// <returns></returns>
    private async Task DoDonation(decimal amount)
    {
        new NotImplementedException();
    }

    /// <summary>
    /// Handle Appearing event
    /// </summary>
    [RelayCommand]
    private void OnAppearing()
    {
        VersionNo = AppInfo.Current.VersionString;
        //DeviceId = Preferences.Get("DeviceId", "");
        //Warehouse = Preferences.Get("DeviceWarehouseId", "");
        _userDialogs.Alert("This is Alert dialog", "Alert dialog", "Understand", "dotnet_bot.png");
    }
}

