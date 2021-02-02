using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using QuickStockTaker.ViewModels.Base;
using QuickStockTaker.Views;
using Xamarin.Forms;

namespace QuickStockTaker.ViewModels
{
    class DashboardViewModel:BaseViewModel
    {
        public ICommand NewStocktakeCmd { get; set; }
        public ICommand EnterDataCmd { get; set; }
        public ICommand BayListCmd { get; set; }
        public ICommand ReviewCmd { get; set; }
        public ICommand SendDataCmd { get; set; }
        bool _canNavigate = true;
        public bool CanNavigate
        {
            get { return _canNavigate; }
            set
            {
                _canNavigate = value;
                OnPropertyChanged();
                RefreshCanExecutes();
            }
        }

        public DashboardViewModel()
        {
            NewStocktakeCmd = new Command(async () => await OnNewStocktakeCmd(), ()=>CanNavigate);
            EnterDataCmd = new Command(async () => await OnEnterDataCmd(), () => CanNavigate);
            BayListCmd = new Command(async () => await OnBayListCmd(), () => CanNavigate);
            ReviewCmd = new Command(async () => await OnReviewCmd(), () => CanNavigate);
            SendDataCmd = new Command(async () => await OnSendDataCmd(), () => CanNavigate);

        }

        private async Task OnSendDataCmd()
        {
            CanNavigate = false;
            await Shell.Current.GoToAsync($"{nameof(DataUploadPage)}");
            CanNavigate = true;
        }

        private async Task OnReviewCmd()
        {
            CanNavigate = false;
            await Shell.Current.GoToAsync($"{nameof(ReviewPage)}");
            CanNavigate = true;

        }

        private async Task OnBayListCmd()
        {
            CanNavigate = false;
            await Shell.Current.GoToAsync($"{nameof(BayListPage)}");
            CanNavigate = true;

        }

        private async Task OnEnterDataCmd()
        {
            CanNavigate = false;
            await Shell.Current.GoToAsync($"{nameof(EnterDatePage)}");
            CanNavigate = true;
        }

        private async Task OnNewStocktakeCmd()
        {
            CanNavigate = false;
            await Shell.Current.GoToAsync($"{nameof(NewStocktakePage)}");
            CanNavigate = true;
        }

        void RefreshCanExecutes()
        {
            (NewStocktakeCmd as Command)?.ChangeCanExecute();
            (EnterDataCmd as Command)?.ChangeCanExecute();
            (BayListCmd as Command)?.ChangeCanExecute();
            (SendDataCmd as Command)?.ChangeCanExecute();

        }
    }
}
