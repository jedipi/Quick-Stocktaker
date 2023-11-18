using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.Core.Data;
using CommunityToolkit.Mvvm.Input;


namespace QuickStockTaker.Core.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        public DashboardViewModel() { }

        [RelayCommand]
        private async Task OnSendData()
        {
            //await Shell.Current.GoToAsync($"{nameof(DataUploadPage)}");
        }

        [RelayCommand]
        private async Task OnReview()
        {
            //await Shell.Current.GoToAsync($"{nameof(ReviewPage)}");
        }

        [RelayCommand]
        private async Task OnBayList()
        {
            //await Shell.Current.GoToAsync($"{nameof(BayListPage)}");

        }

        [RelayCommand]
        private async Task OnEnterData()
        {
            if (Preferences.Get(Constants.StocktakeNumber, "") == "")
            {
                //await _dialogs.AlertAsync("Please specify a stocktake number before scanning any item", "Error");
                return;
            }

            //await Shell.Current.GoToAsync($"{nameof(EnterDatePage)}");

        }

        [RelayCommand]
        private async Task OnNewStocktake()
        {

            await Shell.Current.GoToAsync($"NewStocktakePage");

        }
    }
}
