using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickStockTaker.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace QuickStockTaker.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomeTabPage : ContentPage
    {
        private HomeTabViewModel _vm;
        public HomeTabPage()
        {
            InitializeComponent();
            BindingContext = _vm = new HomeTabViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _vm.Initiate(); // need to reload every time it appears in case it changed in main menu (ex: change date).

            
        }
    }
}