using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace QuickStockTaker.ViewModels
{
    class SettingViewModel : BaseViewModel
    {
        public int DeviceId
        {
            get => Preferences.Get("DeviceId", 0);
            set
            {
                if (Preferences.Get("DeviceId", 0) == value)
                    return;

                Preferences.Set("DeviceId", value);
            }
        }


        public bool ContinuousMode
        {
            get => Preferences.Get("ContinuousMode", false);
            set
            {
                if (Preferences.Get("ContinuousMode", false) == value)
                    return;

                Preferences.Set("ContinuousMode", value);
            }
        }




        public SettingViewModel()
        {
            

        }

    }
}
