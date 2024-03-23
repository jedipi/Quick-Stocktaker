using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using QuickStockTaker.Core.Popups;
using QuickStockTaker.Core.ViewModels;
using ZXing.Net.Maui;

namespace QuickStockTaker.Views;

public partial class EnterDatePage : ContentPage
{
    public EnterDatePage(EnterDateViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;

    }
}