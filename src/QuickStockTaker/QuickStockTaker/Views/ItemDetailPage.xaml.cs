using QuickStockTaker.ViewModels;
using System.ComponentModel;
using Autofac;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Forms;

namespace QuickStockTaker.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = ViewModelLocator.Container.Resolve<ItemDetailViewModel>();
        }
        protected override bool OnBackButtonPressed() => false;
    }
}