using System;
using System.ComponentModel;
using Autofac;
using QuickStockTaker.ViewModels;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace QuickStockTaker.Views
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
            BindingContext = ViewModelLocator.Container.Resolve<AboutViewModel>();
        }
    }
}