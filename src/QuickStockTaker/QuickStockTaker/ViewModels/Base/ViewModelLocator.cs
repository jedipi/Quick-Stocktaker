using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Acr.UserDialogs;
using Autofac;
using NLog;
using QuickStockTaker.DataAccess;
using QuickStockTaker.Models;
using QuickStockTaker.Services;
using ZXing.Mobile;

namespace QuickStockTaker.ViewModels.Base
{
    /// <summary>
    /// Setup IoC container with Autofac
    /// </summary>
    public static class ViewModelLocator
    {
        public static IContainer Container { get; }

        static ViewModelLocator()
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

            builder.RegisterInstance(NLog.LogManager.GetCurrentClassLogger()).As<ILogger>();
            builder.RegisterInstance(UserDialogs.Instance).As<IUserDialogs>().SingleInstance();
            builder.RegisterType<DBConnection>().As<IDBConnection>().SingleInstance();
            builder.RegisterType<DataExportFactory>();
            builder.RegisterType<EmailService>();
            
            builder.RegisterType<MobileBarcodeScanner>().As<IMobileBarcodeScanner>();
        }
    }
}
