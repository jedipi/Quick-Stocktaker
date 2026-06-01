using CommunityToolkit.Mvvm.ComponentModel;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services.Interfaces;
using Serilog;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IAppPreferences _preferences;

        /// <summary>
        /// set device/scanner id
        /// </summary>
        public string DeviceId
        {
            get => _preferences.GetString(Constants.DeviceId, "");
            set
            {
                if (_preferences.GetString(Constants.DeviceId, "") == value)
                    return;

                _preferences.Set(Constants.DeviceId, value);
            }
        }

        /// <summary>
        /// enable continuous scan mode
        /// </summary>
        public bool ContinuousMode
        {
            get => _preferences.GetBool(Constants.ContinuousMode, false);
            set
            {
                if (_preferences.GetBool(Constants.ContinuousMode, false) == value)
                    return;

                _preferences.Set(Constants.ContinuousMode, value);
            }
        }

        public SettingsViewModel(IAppPreferences preferences)
        {
            _preferences = preferences;
            Log.Information("Start SettingsViewModel");
        }
    }
}
