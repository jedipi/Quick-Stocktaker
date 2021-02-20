using QuickStockTaker.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Acr.UserDialogs;
using NLog;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Forms;

namespace QuickStockTaker.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        public Command LoginCommand { get; }

        public LoginViewModel(IUserDialogs dialogs, ILogger logger) : base(dialogs, logger)
        {
            LoginCommand = new Command(OnLoginClicked);
        }

        private async void OnLoginClicked(object obj)
        {
            // Prefixing with `//` switches to a different navigation stack instead of pushing to the active one
            await Shell.Current.GoToAsync($"//{nameof(AboutPage)}");
        }
    }
}
