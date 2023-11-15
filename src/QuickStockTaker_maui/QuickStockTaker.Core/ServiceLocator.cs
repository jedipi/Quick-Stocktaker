﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autofac;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Repositories;
using QuickStockTaker.Core.Models;
using QuickStockTaker.Core.Services;
using IContainer = Autofac.IContainer;
//using ZXing.Mobile;

namespace QuickStockTaker.Core
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
            // main assembly
            var mainAccess = AppDomain.CurrentDomain.GetAssemblies()
                .First(x => x.Location.Contains("QuickStockTaker.dll"));

            builder.RegisterAssemblyTypes(mainAccess)
                .Where(t => t.Name.EndsWith("Validator"));

            builder.RegisterAssemblyTypes(mainAccess)
                .Where(t => t.Name.EndsWith("Service"))
                .AsImplementedInterfaces();


            // register core assembly  ========================
            var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .First(x => x.Location.Contains("QuickStockTaker.Core.dll"));

            builder.RegisterAssemblyTypes(coreAssembly)
                .Where(t => t.Name.EndsWith("Repository"))
                .AsImplementedInterfaces();

            builder.RegisterAssemblyTypes(coreAssembly)
                .Where(t => t.Name.EndsWith("ViewModel"));

            // register individule class ======================
            //builder.RegisterInstance(LogManager.GetCurrentClassLogger()).As<ILogger>();

            // https://autofac.readthedocs.io/en/latest/best-practices/index.html#register-frequently-used-components-with-lambdas
            // use lambda to register freqquently-used components can improvde speed
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