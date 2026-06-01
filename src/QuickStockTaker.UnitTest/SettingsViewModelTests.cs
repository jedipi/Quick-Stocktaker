using FluentAssertions;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.UnitTest;

public class SettingsViewModelTests
{
    [Fact]
    public void Settings_DefaultValues_MatchCurrentPreferenceDefaults()
    {
        var viewModel = new SettingsViewModel(new TestAppPreferences());

        viewModel.DeviceId.Should().BeEmpty();
        viewModel.ContinuousMode.Should().BeFalse();
    }

    [Fact]
    public void Settings_WhenValuesAreSet_PersistsToPreferences()
    {
        var preferences = new TestAppPreferences();
        var viewModel = new SettingsViewModel(preferences);

        viewModel.DeviceId = "SCANNER-01";
        viewModel.ContinuousMode = true;

        preferences.GetString(Constants.DeviceId, "").Should().Be("SCANNER-01");
        preferences.GetBool(Constants.ContinuousMode, false).Should().BeTrue();

        var reloadedViewModel = new SettingsViewModel(preferences);
        reloadedViewModel.DeviceId.Should().Be("SCANNER-01");
        reloadedViewModel.ContinuousMode.Should().BeTrue();
    }
}
