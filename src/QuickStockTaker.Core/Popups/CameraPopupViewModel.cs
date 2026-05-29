using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.Popups
{
    public partial class CameraPopupViewModel : ObservableObject, IQueryAttributable
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

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue(nameof(IsScanContinuously), out var isScanContinuously))
                IsScanContinuously = (bool)isScanContinuously;

            if (query.TryGetValue(nameof(DelayBetweenContinuousScans), out var delayBetweenContinuousScans))
                DelayBetweenContinuousScans = (int)delayBetweenContinuousScans;
        }
    }
}
