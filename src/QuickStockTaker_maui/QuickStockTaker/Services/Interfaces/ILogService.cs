using System.Reflection;

//using NLog;

namespace QuickStockTaker.Services.Interfaces
{
    /// <summary>
    /// This interface provides logging service interface.
    /// </summary>
    public interface ILogService
    {
        void Initialize(Assembly assembly, string assemblyName);
    }
}
