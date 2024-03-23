using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class DataUploadPage : ContentPage
{
	public DataUploadPage(DataUploadViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}