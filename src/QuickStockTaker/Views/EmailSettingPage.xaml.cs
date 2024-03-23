using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class EmailSettingPage : ContentPage
{
	public EmailSettingPage(EmailSettingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}