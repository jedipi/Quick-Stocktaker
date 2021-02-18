using System;
using System.Reflection;
using Acr.UserDialogs;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Provider;
using Android.Util;
using Autofac;
using FFImageLoading;
using FFImageLoading.Forms.Platform;
using Lottie.Forms.Platforms.Android;
using MediaManager;
using NLog;
using QuickStockTaker.Common.Services;
using QuickStockTaker.Services;
using QuickStockTaker.ViewModels.Base;

namespace QuickStockTaker.Droid
{
    [Activity(Label = "QuickStockTaker", Icon = "@mipmap/icon", Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private NLog.ILogger _logger;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            // initialse Nlog
            new LoggerService().InitializeNLog();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            global::ZXing.Net.Mobile.Forms.Android.Platform.Init();
            ZXing.Mobile.MobileBarcodeScanner.Initialize(Application);

            // ffimageloading
            ImageService.Instance.Initialize();
            CachedImageRenderer.Init(true);
            CachedImageRenderer.InitImageViewHandler();

            
            UserDialogs.Init(this);

            // media player
            CrossMediaManager.Current.Init(this);

            LoadApplication(new App());
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            global::ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger = ViewModelLocator.Container.Resolve<ILogger>();
            _logger.Error(e.ExceptionObject as Exception, "Unhandled Exception");
        }

    }
}