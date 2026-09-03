using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class SavedStocktakesPage : ContentPage
{
    public SavedStocktakesPage(SavedStocktakesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
