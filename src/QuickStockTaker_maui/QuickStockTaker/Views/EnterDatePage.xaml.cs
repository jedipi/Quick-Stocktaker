using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class EnterDatePage : ContentPage
{
	public EnterDatePage(IServiceProvider provider)
	{
		InitializeComponent();
		BindingContext = provider.GetService(typeof(EnterDateViewModel));
	}
}