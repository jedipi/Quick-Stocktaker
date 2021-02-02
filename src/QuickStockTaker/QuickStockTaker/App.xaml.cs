using QuickStockTaker.Services;
using QuickStockTaker.Views;
using System;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Autofac;
using NLog;
using QuickStockTaker.DataAccess;
using QuickStockTaker.Interfaces.Logging;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

namespace QuickStockTaker
{
    public partial class App : Application
    {
        //private readonly NLog.ILogger _logger = NLog.LogManager.GetCurrentClassLogger();


        public App()
        {
            InitializeComponent();
            //Device.SetFlags(new string[] { "Brush_Experimental" });
            //DependencyService.Register<MockDataStore>();

            var logger = ViewModelLocator.Container.Resolve<ILogger>();
            logger.Info("App start");

            var dbConnection = ViewModelLocator.Container.Resolve<IDBConnection>();
            var db = new DataAccess.SQLiteDb(dbConnection);
            Task.Run(async () => { await db.Create(); });//.Wait();

            // setup logger
            var deviceId = Preferences.Get("DeviceId", 0) ;
            GlobalDiagnosticsContext.Set("ScannerId",deviceId);
            
            MainPage = new AppShell();
        }

        

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
