using Autofac;
using Autofac.Extensions.DependencyInjection;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core;
using Serilog;
using Serilog.Events;
using System.Net;

namespace QuickStockTaker;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("MaterialIconsOutlined-Regular", "MD-O");
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

        //#if DEBUG
        //        builder.Logging.AddDebug();
        //#endif


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
