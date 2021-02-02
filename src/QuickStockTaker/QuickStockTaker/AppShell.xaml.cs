using QuickStockTaker.ViewModels;
using QuickStockTaker.Views;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Forms;

namespace QuickStockTaker
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        private readonly NLog.ILogger _logger = NLog.LogManager.GetCurrentClassLogger();
        public AppShell()
        {
            InitializeComponent();
            _logger.Info("Application Start.");

            //var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "logs");
            //var files = Directory.GetFiles(folder, "*.log");
            //foreach (var file in files)
            //{

            //    var data = File.ReadAllText(file);
            //}


            Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
            Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));

            Routing.RegisterRoute(nameof(NewStocktakePage), typeof(NewStocktakePage));
            Routing.RegisterRoute(nameof(EnterDatePage), typeof(EnterDatePage));
            Routing.RegisterRoute(nameof(BayListPage), typeof(BayListPage));
            Routing.RegisterRoute(nameof(BayDetailsPage), typeof(BayDetailsPage));
            Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
            Routing.RegisterRoute(nameof(ReviewPage), typeof(ReviewPage));
            Routing.RegisterRoute(nameof(DataUploadPage), typeof(DataUploadPage));
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
