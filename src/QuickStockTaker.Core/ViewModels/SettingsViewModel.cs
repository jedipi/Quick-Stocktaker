using CommunityToolkit.Mvvm.ComponentModel;
using QuickStockTaker.Core.Data;
using Serilog;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        /// <summary>
        /// set device/scanner id
        /// </summary>
        public string DeviceId
        {
            get => Preferences.Get(Constants.DeviceId, "");
            set
            {
                if (Preferences.Get(Constants.DeviceId, "") == value)
                    return;

                Preferences.Set(Constants.DeviceId, value);
            }
        }

        /// <summary>
        /// enable continuous scan mode
        /// </summary>
        public bool ContinuousMode
        {
            get => Preferences.Get(Constants.ContinuousMode, false);
            set
            {
                if (Preferences.Get(Constants.ContinuousMode, false) == value)
                    return;

                Preferences.Set(Constants.ContinuousMode, value);
            }
        }

        public SettingsViewModel()
        {
            Log.Information("Start SettingsViewModel");
        }
    }
}
