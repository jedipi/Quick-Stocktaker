using CommunityToolkit.Maui;
using QuickStockTaker.Core.Popups;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Services;

public class MauiCameraPopupService : ICameraPopupService
{
    private readonly IPopupService _popupService;
    private bool _isPopupOpen;

    public MauiCameraPopupService(IPopupService popupService)
    {
        _popupService = popupService;
    }

    public async Task<CameraPopupResult> ShowForResultAsync(bool isScanContinuously)
    {
        var result = await _popupService.ShowPopupAsync<CameraPopupViewModel, CameraPopupResult>(
            Shell.Current,
            options: PopupOptions.Empty,
            shellParameters: CreateCameraPopupParameters(isScanContinuously));

        return result.WasDismissedByTappingOutsideOfPopup ? null : result.Result;
    }

    public async Task ShowAsync(bool isScanContinuously)
    {
        _isPopupOpen = true;
        try
        {
            await _popupService.ShowPopupAsync<CameraPopupViewModel>(
                Shell.Current,
                options: PopupOptions.Empty,
                shellParameters: CreateCameraPopupParameters(isScanContinuously));
        }
        finally
        {
            _isPopupOpen = false;
        }
    }

    public async Task CloseAsync()
    {
        if (!_isPopupOpen)
            return;

        await _popupService.ClosePopupAsync(Shell.Current);
    }

    private static Dictionary<string, object> CreateCameraPopupParameters(bool isScanContinuously)
    {
        return new Dictionary<string, object>
        {
            [nameof(CameraPopupViewModel.IsScanContinuously)] = isScanContinuously
        };
    }
}
