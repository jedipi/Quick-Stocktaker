using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.Core.Data;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;


namespace QuickStockTaker.Core.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        IUserDialogs _dialogs;
        private readonly IAppPreferences _preferences;
        private readonly INavigationService _navigationService;

        public DashboardViewModel(
            IUserDialogs dialogs,
            IAppPreferences preferences,
            INavigationService navigationService)
        {
            _dialogs = dialogs;
            _preferences = preferences;
            _navigationService = navigationService;
        }

        [RelayCommand]
        private async Task OnSendData()
        {
            await _navigationService.GoToAsync(NavigationRoutes.DataUploadPage);
        }

        [RelayCommand]
        private async Task OnReview()
        {
            await _navigationService.GoToAsync(NavigationRoutes.ReviewPage);
        }

        [RelayCommand]
        private async Task OnBayList()
        {
            await _navigationService.GoToAsync(NavigationRoutes.BayListPage);

        }

        [RelayCommand]
        private async Task OnEnterData()
        {
            if (_preferences.GetString(Constants.StocktakeNumber, "") == "")
            {
                await _dialogs.AlertAsync("Please specify a stocktake number before scanning any item", "Error");
                return;
            }

            await _navigationService.GoToAsync(NavigationRoutes.EnterDatePage);

        }

        [RelayCommand]
        private async Task OnNewStocktake()
        {

            await _navigationService.GoToAsync(NavigationRoutes.NewStocktakePage);

        }
    }
}
