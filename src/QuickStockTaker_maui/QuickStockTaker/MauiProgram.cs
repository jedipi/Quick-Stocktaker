using Autofac;
using Autofac.Extensions.DependencyInjection;
using CommunityToolkit.Maui;
using Controls.UserDialogs.Maui;
using QuickStockTaker.Core;
using Serilog;
using Serilog.Events;

namespace QuickStockTaker;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
            .UseUserDialogs(registerInterface: true, () =>
            {
                //setup your default styles for dialogs
                AlertConfig.DefaultBackgroundColor = Colors.Purple;
#if ANDROID
        AlertConfig.DefaultMessageFontFamily = "OpenSans-Regular.ttf";
#else
                AlertConfig.DefaultMessageFontFamily = "OpenSans-Regular";
#endif

                ToastConfig.DefaultCornerRadius = 15;
            })
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("material-icons-outlined-regular.ttf", "MD-O");
                fonts.AddFont("Font Awesome 6 Duotone-Solid-900.otf", "FA-D");
                fonts.AddFont("Font Awesome 6 Free-Regular-400.otf", "FA");
                fonts.AddFont("Font Awesome 6 Free-Solid-900.otf", "FA-S");
                fonts.AddFont("pro-fa-regular-400.ttf", "FA-Pro");
            });

        builder.ConfigureContainer(new AutofacServiceProviderFactory(), autofacBuilder =>
        {
            // register views
            autofacBuilder.RegisterAssemblyTypes(typeof(MauiProgram).Assembly)
                        .Where(t => t.Name.EndsWith("Page"));
            autofacBuilder.RegisterType<AppShell>().SingleInstance();

            // register view models
            autofacBuilder.RegisterAssemblyTypes(typeof(ServiceLocator).Assembly)
                            .Where(t => t.Name.EndsWith("ViewModel"));

            // register db repository
            autofacBuilder.RegisterAssemblyTypes(typeof(ServiceLocator).Assembly)
                        .Where(t => t.Name.EndsWith("Repository"))
                        .AsImplementedInterfaces();

            // register db repository
            autofacBuilder.RegisterAssemblyTypes(typeof(ServiceLocator).Assembly)
                        .Where(t => t.Name.EndsWith("Service"))
                        .AsImplementedInterfaces();
        });

        SetupSerilog();
        builder.Logging.AddSerilog(dispose: true);

        return builder.Build();
	}

    private static void SetupSerilog()
    {
        
        var flushInterval = new TimeSpan(0, 0, 1);
        var file = Path.Combine(FileSystem.AppDataDirectory, "log.log");
        Log.Logger = new LoggerConfiguration()
             .Enrich.FromLogContext()
             .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
#if DEBUG
            .WriteTo.Debug()
#endif
             .WriteTo.File(file, flushToDiskInterval: flushInterval, encoding: System.Text.Encoding.UTF8, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 22,
              outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} OtherProperties:{OtherProperty}{NewLine}{Exception}")
             .CreateLogger();
    }
}
