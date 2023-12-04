using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class NewStocktakePage : ContentPage
{
	public NewStocktakePage(NewStocktakeViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}