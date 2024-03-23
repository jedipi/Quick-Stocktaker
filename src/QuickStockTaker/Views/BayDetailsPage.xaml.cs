using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class BayDetailsPage : ContentPage
{
    public BayDetailsPage(BayDetailsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override bool OnBackButtonPressed() => false;
}