using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.Views;

public partial class ExportPresetsPage : ContentPage
{
    private readonly ExportPresetsViewModel _viewModel;

    public ExportPresetsPage(ExportPresetsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
