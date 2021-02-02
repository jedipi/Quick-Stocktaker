using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using QuickStockTaker.Data;
using QuickStockTaker.ViewModels.Base;
using QuickStockTaker.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace QuickStockTaker.ViewModels
{
    class HomeTabViewModel:BaseViewModel
    {
        #region Properties

        public string VersionNo => AppInfo.VersionString;
        public int DeviceId { get; set; }

        public int StocktakeNumber { get; set; }
        public string Site { get; set; }
        public string StocktakeDate { get; set; }
        bool _canNavigate = true;
        public bool CanNavigate
        {
            get { return _canNavigate; }
            set
            {
                _canNavigate = value;
                OnPropertyChanged();
                (GetStartedCmd as Command)?.ChangeCanExecute();
            }
        }
        
        #endregion

        #region Commands

        public ICommand GetStartedCmd { get; set; }


        #endregion

        public HomeTabViewModel()
        {
            GetStartedCmd = new Command(async  ()=> await OnGetStartedCmd(), () => CanNavigate);
        }

        private async Task OnGetStartedCmd()
        {
            CanNavigate = false;
            await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
            CanNavigate = true;
        }

        public void Initiate()
        {
            DeviceId = Preferences.Get(Constants.DeviceId, 0);
            StocktakeNumber = Preferences.Get(Constants.StocktakeNumber, 0);
            Site = Preferences.Get(Constants.Site, "");
            var a = Preferences.Get(Constants.StocktakeDate, DateTime.MinValue);
            StocktakeDate = a.ToShortDateString();
        }
    }
}
