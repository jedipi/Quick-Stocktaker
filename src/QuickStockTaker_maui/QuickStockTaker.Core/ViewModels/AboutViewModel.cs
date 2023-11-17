using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    [ObservableProperty]
    private string _versionNo;

    [ObservableProperty]
    private string _deviceId;

    [ObservableProperty]
    private string _warehouse;

    private readonly IUserDialogs _userDialogs;

    public AboutViewModel(IUserDialogs userDialogs)
    {
        _userDialogs = userDialogs;
        Log.Information("Start AboutViewModel");
    }

    /// <summary>
    /// Handle Appearing event
    /// </summary>
    [RelayCommand]
    private void OnAppearing()
    {
        VersionNo = AppInfo.Current.VersionString;
        DeviceId = Preferences.Get("DeviceId", "");
        Warehouse = Preferences.Get("DeviceWarehouseId", "");
        _userDialogs.Alert("This is Alert dialog", "Alert dialog", "Understand", "dotnet_bot.png");
    }
}

