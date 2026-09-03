using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using QuickStockTaker.Core.Data;
using Serilog;
using QuickStockTaker.Core.Validators;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class NewStocktakeViewModel : ObservableObject
    {
        #region fields
        private readonly StocktakeValidator _validator;
        private readonly IAppPreferences _preferences;
        private readonly INavigationService _navigationService;
        private readonly IPageDialogService _pageDialogService;
        private readonly IStocktakeWorkspace _workspace;
        private readonly IEntitlementService _entitlementService;
        private int? _replaceStocktakeId;
        #endregion

        #region Properties

        [ObservableProperty]
        private string _stocktakeNumber;

        [ObservableProperty]
        private string _site;

        [ObservableProperty]
        private DateTime _stocktakeDate;

        [ObservableProperty]
        private bool _isToggled;

        [ObservableProperty]
        private bool _isDetailsVisible;

        #endregion

        public NewStocktakeViewModel(
            StocktakeValidator validator,
            IAppPreferences preferences,
            INavigationService navigationService,
            IPageDialogService pageDialogService,
            IStocktakeWorkspace workspace,
            IEntitlementService entitlementService)
        {
            Log.Information("Start NewStocktakeViewModel");
            _validator = validator;
            _preferences = preferences;
            _navigationService = navigationService;
            _pageDialogService = pageDialogService;
            _workspace = workspace;
            _entitlementService = entitlementService;
        }

        /// <summary>
        /// Save new stocktake setting
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task OnSave()
        {
            if (!IsDetailsVisible)
                return;

            await _entitlementService.InitializeAsync();
            var savedStocktakes = await _workspace.GetSavedAsync();
            if (!_entitlementService.IsPro)
            {
                var replacementIsAuthorized = savedStocktakes.Count == 1 &&
                    _replaceStocktakeId == savedStocktakes[0].Id;
                if (savedStocktakes.Count > 0 && !replacementIsAuthorized)
                {
                    await _pageDialogService.DisplayAlertAsync(
                        "Free Stocktake allowance reached",
                        "Upgrade to Pro, or use the new Stocktake switch to confirm replacement of your single Saved Stocktake.",
                        "OK");
                    return;
                }
            }

            // validat all inputs
            var validateResult = _validator.Validate(this);
            if (!validateResult.IsValid)
            {
                await _pageDialogService.DisplayAlertAsync(
                    "Error",
                    validateResult.Errors.First().ErrorMessage,
                    "OK");
                return;
            }

            var replaceStocktakeId = _entitlementService.IsPro ? null : _replaceStocktakeId;
            await _workspace.CreateAsync(
                new StocktakeDefinition(StocktakeNumber, Site, StocktakeDate),
                replaceStocktakeId);

            // go back to previous page
            await _navigationService.GoToAsync(NavigationRoutes.Back);
        }


        [RelayCommand]
        private async Task OnToggled()
        {
            if (!IsToggled)
            {
                _replaceStocktakeId = null;
                IsDetailsVisible = false;
                return;
            }

            // check device id
            var deviceId = _preferences.GetString(Constants.DeviceId, "");
            if (string.IsNullOrEmpty(deviceId))
            {
                await _pageDialogService.DisplayAlertAsync(
                    "ERROR",
                    "Invalid Device ID. Please setup a Device ID before start a new stocktake.",
                    "OK");
                return;
            }

            await _entitlementService.InitializeAsync();
            var savedStocktakes = await _workspace.GetSavedAsync();
            _replaceStocktakeId = null;

            if (!_entitlementService.IsPro && savedStocktakes.Count > 0)
            {
                if (savedStocktakes.Count > 1)
                {
                    var overAllowanceAction = await _pageDialogService.DisplayActionSheetAsync(
                        "Free Stocktake allowance reached",
                        "Cancel",
                        null,
                        "Upgrade to Pro",
                        "Manage Saved Stocktakes");
                    IsToggled = false;

                    if (overAllowanceAction == "Upgrade to Pro")
                        await _navigationService.GoToAsync($"//{NavigationRoutes.AboutPage}");
                    else if (overAllowanceAction == "Manage Saved Stocktakes")
                        await _navigationService.GoToAsync(NavigationRoutes.SavedStocktakesPage);

                    return;
                }

                var action = await _pageDialogService.DisplayActionSheetAsync(
                    "Free Stocktake allowance reached",
                    "Cancel",
                    null,
                    "Upgrade to Pro",
                    "Replace current Stocktake");
                if (action == "Upgrade to Pro")
                {
                    IsToggled = false;
                    await _navigationService.GoToAsync($"//{NavigationRoutes.AboutPage}");
                    return;
                }

                if (action != "Replace current Stocktake")
                {
                    IsToggled = false;
                    return;
                }

                var replaceConfirmed = await _pageDialogService.DisplayConfirmationAsync(
                    "Replace current Stocktake",
                    "The current Stocktake and all of its items will be permanently deleted only after the replacement is saved successfully.",
                    "Continue",
                    "Cancel");
                if (!replaceConfirmed)
                {
                    IsToggled = false;
                    return;
                }

                _replaceStocktakeId = savedStocktakes.Single().Id;
            }

            // initialise values
            StocktakeNumber = "";
            Site = "";
            StocktakeDate = DateTime.Today;

            IsDetailsVisible = true;
        }
    }
}
