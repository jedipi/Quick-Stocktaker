using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    public AboutViewModel()
    {
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
    }
}

