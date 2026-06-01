using CommunityToolkit.Mvvm.ComponentModel;
using QuickStockTaker.Core.Data;
using Controls.UserDialogs.Maui;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class HomeTabViewModel : ObservableObject
    {
        #region Fields
        //private bool _shown;
        private readonly ILogger<HomeTabViewModel> _logger;
        private readonly IAppPreferences _preferences;
        private readonly INavigationService _navigationService;
        #endregion

        #region Properties

        public string VersionNo => AppInfo.VersionString;

        [ObservableProperty]
        private string _deviceId;
        //public string DeviceId { get; set; }

        [ObservableProperty]
        private string _stocktakeNumber;
        //public int StocktakeNumber { get; set; }

        [ObservableProperty]
        private string _site;
        //public string Site { get; set; }

        [ObservableProperty]
        private string _stocktakeDate;
        //public string StocktakeDate { get; set; }

        #endregion

        public HomeTabViewModel(
            IAppPreferences preferences,
            INavigationService navigationService,
            ILogger<HomeTabViewModel> logger)
        {
            Log.Information("Start HomeTabViewModel");
            _preferences = preferences;
            _navigationService = navigationService;
            _logger = logger;
        }

        [RelayCommand]
        private async Task OnGetStarted()
        {
            await _navigationService.GoToAsync(NavigationRoutes.DashboardRoot);
        }


        /// <summary>
        /// Handle Appearing event
        /// </summary>
        [RelayCommand]
        private void OnAppearing()
        {
            DeviceId = _preferences.GetString(Constants.DeviceId, "");
            StocktakeNumber = _preferences.GetString(Constants.StocktakeNumber, "");
            Site = _preferences.GetString(Constants.Site, "");
            var a = _preferences.GetDateTime(Constants.StocktakeDate, DateTime.MinValue);
            StocktakeDate = a.ToShortDateString();
        }
    }
}
