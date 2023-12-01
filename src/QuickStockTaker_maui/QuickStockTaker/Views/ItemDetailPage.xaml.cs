using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class ItemDetailPage : ContentPage
{
	public ItemDetailPage(ItemDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}