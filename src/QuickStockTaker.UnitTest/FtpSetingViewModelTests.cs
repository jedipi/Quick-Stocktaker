using FluentAssertions;
using QuickStockTaker.Core.ViewModels;

namespace QuickStockTaker.UnitTest;

public class FtpSetingViewModelTests
{
    [Fact]
    public void GetPortForProtocolChange_WhenExistingPortIsPreviousDefault_UsesNewProtocolDefault()
    {
        var port = FtpSetingViewModel.GetPortForProtocolChange(
            previousUseSftp: true,
            nextUseSftp: false,
            currentPort: "22");

        port.Should().Be("21");
    }

    [Fact]
    public void GetPortForProtocolChange_WhenExistingPortIsCustom_KeepsCustomPort()
    {
        var port = FtpSetingViewModel.GetPortForProtocolChange(
            previousUseSftp: true,
            nextUseSftp: false,
            currentPort: "8022");

        port.Should().Be("8022");
    }
}
