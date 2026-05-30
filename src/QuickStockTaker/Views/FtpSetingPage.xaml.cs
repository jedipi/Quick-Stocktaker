using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class FtpSetingPage : ContentPage
{
    public FtpSetingPage(FtpSetingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
