using CommunityToolkit.Mvvm.ComponentModel;
using QuickStockTaker.Core.Data;
using Controls.UserDialogs.Maui;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
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
