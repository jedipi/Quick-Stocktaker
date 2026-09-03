using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;


namespace QuickStockTaker.Core.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        IUserDialogs _dialogs;
        private readonly INavigationService _navigationService;
        private readonly IStocktakeWorkspace _workspace;

        public DashboardViewModel(
            IUserDialogs dialogs,
            INavigationService navigationService,
            IStocktakeWorkspace workspace)
        {
            _dialogs = dialogs;
            _navigationService = navigationService;
            _workspace = workspace;
        }

        [RelayCommand]
        private async Task OnSendData()
        {
            if (!await EnsureCurrentStocktakeAsync()) return;
            await _navigationService.GoToAsync(NavigationRoutes.DataUploadPage);
        }

        [RelayCommand]
        private async Task OnReview()
        {
            if (!await EnsureCurrentStocktakeAsync()) return;
            await _navigationService.GoToAsync(NavigationRoutes.ReviewPage);
        }

        [RelayCommand]
        private async Task OnBayList()
        {
            if (!await EnsureCurrentStocktakeAsync()) return;
            await _navigationService.GoToAsync(NavigationRoutes.BayListPage);

        }

        [RelayCommand]
        private async Task OnEnterData()
        {
            if (!await EnsureCurrentStocktakeAsync()) return;

            await _navigationService.GoToAsync(NavigationRoutes.EnterDatePage);

        }

        [RelayCommand]
        private async Task OnNewStocktake()
        {

            await _navigationService.GoToAsync(NavigationRoutes.NewStocktakePage);

        }

        [RelayCommand]
        private Task OnSavedStocktakes() =>
            _navigationService.GoToAsync(NavigationRoutes.SavedStocktakesPage);

        private async Task<bool> EnsureCurrentStocktakeAsync()
        {
            if (await _workspace.GetCurrentAsync() is not null)
                return true;

            await _dialogs.AlertAsync(
                "Create or select a Saved Stocktake first.",
                "No Current Stocktake");
            return false;
        }
    }
}
