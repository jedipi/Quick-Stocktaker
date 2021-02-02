using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;



namespace QuickStockTaker.Common.Services
{
    public class LoggerService 
    {
        public void InitializeNLog()
        {
            Assembly assembly = this.GetType().Assembly;
            string assemblyName = assembly.GetName().Name;
            new QuickStockTaker.Services.LogService().Initialize(assembly, assemblyName);
        }
    }
}
