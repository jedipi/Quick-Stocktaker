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

namespace QuickStockTaker.Core.ViewModels
{
    public partial class HomeTabViewModel : ObservableObject
    {
        #region Fields
        private bool _shown;
        IServiceProvider _provider;
        private readonly ILogger<HomeTabViewModel> _logger;
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

        public HomeTabViewModel(IServiceProvider provider, ILogger<HomeTabViewModel> logger) 
        {
            Log.Information("Start HomeTabViewModel");
            _provider = provider;
            _logger = logger;
        }

        [RelayCommand]
        private async Task OnGetStarted()
        {
            await Shell.Current.GoToAsync($"//DashboardPage");
        }


        /// <summary>
        /// Handle Appearing event
        /// </summary>
        [RelayCommand]
        private void OnAppearing()
        {
            DeviceId = Preferences.Get(Constants.DeviceId, "");
            StocktakeNumber = Preferences.Get(Constants.StocktakeNumber, "");
            Site = Preferences.Get(Constants.Site, "");
            var a = Preferences.Get(Constants.StocktakeDate, DateTime.MinValue);
            StocktakeDate = a.ToShortDateString();
        }

    }
}
