using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Acr.UserDialogs;
using Autofac;
using NLog;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Repositories;
using QuickStockTaker.Models;
using QuickStockTaker.Services;
using IContainer = Autofac.IContainer;
//using ZXing.Mobile;

namespace QuickStockTaker
{
    /// <summary>
    /// Setup IoC container with Autofac
    /// </summary>
    public static class ServiceLocator
    {
        public static IContainer Container { get; }

        static ServiceLocator()
        {
            ContainerBuilder builder = new ContainerBuilder();
            RegisterType(builder);
            Container = builder.Build();
        }

        // register all viewmodels ans services 
        static void RegisterType(ContainerBuilder builder)
        {
            //_builder.RegisterType<RequestProvider>().As<IRequestProvider>();
            //builder.RegisterType<OrderService>().As<IOrderService>().SingleInstance();
            var dataAccess = Assembly.GetAssembly(typeof(App));

            builder.RegisterAssemblyTypes(dataAccess)
                .Where(t => t.Name.EndsWith("ViewModel"));

            builder.RegisterAssemblyTypes(dataAccess)
                .Where(t => t.Name.EndsWith("Validator"));

            builder.RegisterAssemblyTypes(dataAccess)
                .Where(t => t.Name.EndsWith("Service"))
                .AsImplementedInterfaces();

            builder.RegisterAssemblyTypes(dataAccess)
                .Where(t => t.Name.EndsWith("Repository"))
                .AsImplementedInterfaces();

            builder.RegisterInstance(LogManager.GetCurrentClassLogger()).As<ILogger>();

            builder.Register(x => new SQLiteDB(Path.Combine(FileSystem.AppDataDirectory, "StockTacker.db3")))
                .InstancePerLifetimeScope();

            builder.RegisterGeneric(typeof(SQLiteRepository<>))
               .As(typeof(ISQLiteRepository<>))
               .InstancePerLifetimeScope();

            builder.RegisterType<DataExportFactory>();
            builder.RegisterType<EmailService>();

            //builder.RegisterType<MobileBarcodeScanner>().As<IMobileBarcodeScanner>();
        }
    }
}
