using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class NewStocktakePage : ContentPage
{
	public NewStocktakePage(IServiceProvider provider)
	{
		InitializeComponent();
		BindingContext = provider.GetService<NewStocktakeViewModel>();
	}
}