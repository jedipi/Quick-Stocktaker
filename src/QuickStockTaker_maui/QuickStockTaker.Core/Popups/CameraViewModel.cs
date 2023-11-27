using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing.Net.Maui;

namespace QuickStockTaker.Core.Popups
{
    public partial class CameraViewModel : ObservableObject
    {
        public CameraViewModel() { }

        //private TaskCompletionSource<BarcodeResult[]> scanTask = new TaskCompletionSource<BarcodeResult[]>();
        //public Task<BarcodeResult[]> WaitForResultAsync()
        //{
        //    return scanTask.Task;
        //}

        //[RelayCommand]
        //private void OnBarcodesDetected(BarcodeDetectionEventArgs e)
        //{
            
        //    //MainThread.BeginInvokeOnMainThread(async () =>
        //    //{
        //    //    await Application.Current.MainPage.Navigation.PopModalAsync();
        //    //});

        //    scanTask.TrySetResult(e.Results);
        //}
    }
}
