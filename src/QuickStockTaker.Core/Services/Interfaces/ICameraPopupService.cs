using QuickStockTaker.Core.Popups;

namespace QuickStockTaker.Core.Services.Interfaces
{
    public interface ICameraPopupService
    {
        Task<CameraPopupResult> ShowForResultAsync(bool isScanContinuously);

        Task ShowAsync(bool isScanContinuously);
    }
}
