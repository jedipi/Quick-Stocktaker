using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.ViewModels;

public partial class SavedStocktakesViewModel : ObservableObject
{
    private readonly IStocktakeWorkspace _workspace;
    private readonly IEntitlementService _entitlementService;
    private readonly INavigationService _navigationService;
    private readonly IPageDialogService _pageDialogService;

    [ObservableProperty]
    private IReadOnlyList<SavedStocktakeSummary> _stocktakes = [];

    [ObservableProperty]
    private bool _showUpgradePrompt;

    public SavedStocktakesViewModel(
        IStocktakeWorkspace workspace,
        IEntitlementService entitlementService,
        INavigationService navigationService,
        IPageDialogService pageDialogService)
    {
        _workspace = workspace;
        _entitlementService = entitlementService;
        _navigationService = navigationService;
        _pageDialogService = pageDialogService;
    }

    [RelayCommand]
    private async Task OnAppearing()
    {
        await _entitlementService.InitializeAsync();
        await LoadAsync();
        ShowUpgradePrompt = !_entitlementService.IsPro;
    }

    [RelayCommand]
    private async Task OnResume(SavedStocktakeSummary stocktake)
    {
        if (stocktake is null)
            return;

        if (await _workspace.SelectAsync(stocktake.Id))
            await _navigationService.GoToAsync(NavigationRoutes.Back);
    }

    [RelayCommand]
    private async Task OnDelete(SavedStocktakeSummary stocktake)
    {
        if (stocktake is null)
            return;

        var confirmed = await _pageDialogService.DisplayConfirmationAsync(
            "Delete Saved Stocktake",
            $"Delete Stocktake {stocktake.StocktakeNumber} at {stocktake.Site}? This cannot be undone.",
            "Delete",
            "Cancel");
        if (!confirmed)
            return;

        await _workspace.DeleteAsync(stocktake.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private Task OnUpgrade() =>
        _navigationService.GoToAsync($"//{NavigationRoutes.AboutPage}");

    private async Task LoadAsync()
    {
        Stocktakes = await _workspace.GetSavedAsync();
    }
}
