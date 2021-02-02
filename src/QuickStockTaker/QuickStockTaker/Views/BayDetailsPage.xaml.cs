using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using QuickStockTaker.ViewModels;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace QuickStockTaker.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BayDetailsPage : ContentPage
    {
        private BayDetailsViewModel _vm;
        private bool _shown;

        public BayDetailsPage()
        {
            InitializeComponent();
            BindingContext = _vm = ViewModelLocator.Container.Resolve<BayDetailsViewModel>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            //SearchBarSearch.Focus();
            _vm.GetTotalCount();
            if (_shown)
                return;

            _shown = true;

            await _vm.PopulateItems();
        }
    }
}