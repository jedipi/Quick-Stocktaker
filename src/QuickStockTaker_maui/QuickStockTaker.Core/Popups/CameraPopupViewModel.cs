using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.Popups
{
    public partial class CameraPopupViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isScanContinuously;

        [ObservableProperty]
        private int _delayBetweenContinuousScans;
        public CameraPopupViewModel() 
        {
            // default delay in ms
            DelayBetweenContinuousScans = 1500;
        }  

        public void SetIsScanContinuously(bool value)
        {
            IsScanContinuously = value;
        }

        public void SetDelayBetweenContinuousScans(int value)
        {
            DelayBetweenContinuousScans = value;
        }
    }
}
