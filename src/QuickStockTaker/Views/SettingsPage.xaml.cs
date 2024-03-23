using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class SettingsPage : ContentPage
{
	private SettingsViewModel _vm;
	public SettingsPage(SettingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}