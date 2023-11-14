using Autofac;
using Autofac.Extensions.DependencyInjection;
using CommunityToolkit.Maui;
using QuickStockTaker.Core;

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

       


        return builder.Build();
	}
}
