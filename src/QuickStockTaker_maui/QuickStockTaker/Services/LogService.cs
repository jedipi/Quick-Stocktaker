using System;
using System.Reflection;
using System.IO;
using QuickStockTaker.Services.Interfaces;

namespace QuickStockTaker.Services
{
    public class LogService : ILogService
    {
        /// <summary>
        /// Initialise the logger
        /// </summary>
        /// <param name="assembly">assembly</param>
        /// <param name="assemblyName">assembly name</param>
        public void Initialize(Assembly assembly, string assemblyName)
        {
            string resourcePrefix;
            if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
                resourcePrefix = "QuickStockTaker.iOS";
            else if (DeviceInfo.Current.Platform == DevicePlatform.Android)
                resourcePrefix = "QuickStockTaker.Droid";
            else
                throw new Exception("Could not initialize Logger: Unknonw Platform");
            
            string location = $"{resourcePrefix}.NLog.config";
            
            Stream stream = assembly.GetManifestResourceStream(location);
            if (stream == null)
                throw new Exception($"The resource '{location}' was not loaded properly.");
            
            //LogManager.Configuration = new XmlLoggingConfiguration(System.Xml.XmlReader.Create(stream), null);
        }
    }
}
