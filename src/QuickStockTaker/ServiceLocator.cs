﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autofac;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using QuickStockTaker.Core.Repositories;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services;
using IContainer = Autofac.IContainer;
//using ZXing.Mobile;

namespace QuickStockTaker
{
    /// <summary>
    /// Setup IoC container with Autofac
    /// </summary>
    public static class ServiceLocator
    {
        //public static IContainer Container { get; }

        static ServiceLocator()
        {
            //ContainerBuilder builder = new ContainerBuilder();
            //RegisterType(builder);
            //Container = builder.Build();
        }

        // register all viewmodels ans services 
        public static void RegisterType(ContainerBuilder autofacBuilder)
        {
            // register pages
            autofacBuilder.RegisterAssemblyTypes(typeof(MauiProgram).Assembly)
                        .Where(t => t.Name.EndsWith("Page"));

            // register view models
            autofacBuilder.RegisterAssemblyTypes(typeof(Constants).Assembly)
                            .Where(t => t.Name.EndsWith("ViewModel"));

            // register view pages
            autofacBuilder.RegisterAssemblyTypes(typeof(Constants).Assembly)
                           .Where(t => t.Name.EndsWith("Page"));

            // register services
            autofacBuilder.RegisterAssemblyTypes(typeof(Constants).Assembly)
                        .Where(t => t.Name.EndsWith("Service"))
                        .AsImplementedInterfaces();

            autofacBuilder.RegisterAssemblyTypes(typeof(Constants).Assembly)
                .Where(t => t.Name.EndsWith("Validator"));

            // register db and repository
            // https://autofac.readthedocs.io/en/latest/best-practices/index.html#register-frequently-used-components-with-lambdas
            // use lambda to register freqquently-used components can improvde speed
            autofacBuilder.Register(x => new SQLiteDB(Path.Combine(FileSystem.AppDataDirectory, "StockTacker.db3")))
                .InstancePerLifetimeScope();

            autofacBuilder.RegisterGeneric(typeof(SQLiteRepository<>))
               .As(typeof(ISQLiteRepository<>))
               .InstancePerLifetimeScope();

            autofacBuilder.RegisterType<DataExportFactory>();
            autofacBuilder.RegisterType<EmailService>();
        }
    }
}
