using FluentAssertions;
using QuickStockTaker.Core.Services;

namespace QuickStockTaker.UnitTest;

public class FtpUplodServiceTests
{
    [Theory]
    [InlineData("1")]
    [InlineData("21")]
    [InlineData("65535")]
    public void IsValidPort_WhenPortIsInRange_ReturnsTrue(string port)
    {
        FtpUplodService.IsValidPort(port).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("65536")]
    [InlineData("abc")]
    public void IsValidPort_WhenPortIsMissingOrOutOfRange_ReturnsFalse(string port)
    {
        FtpUplodService.IsValidPort(port).Should().BeFalse();
    }

    [Theory]
    [InlineData("", "stock.csv", "stock.csv")]
    [InlineData(" exports / daily ", "stock.csv", "exports/daily/stock.csv")]
    [InlineData("/exports/daily", "stock.csv", "/exports/daily/stock.csv")]
    public void BuildRemotePath_JoinsFolderAndFileName(string folder, string fileName, string expected)
    {
        FtpUplodService.BuildRemotePath(folder, fileName, path => path).Should().Be(expected);
    }

    [Fact]
    public void BuildSftpDirectoryPaths_WhenRemotePathIsRelative_ReturnsIncrementalDirectories()
    {
        FtpUplodService.BuildSftpDirectoryPaths("exports/daily/stock.csv")
            .Should()
            .Equal("exports", "exports/daily");
    }

    [Fact]
    public void BuildSftpDirectoryPaths_WhenRemotePathIsRooted_ReturnsRootedIncrementalDirectories()
    {
        FtpUplodService.BuildSftpDirectoryPaths("/exports/daily/stock.csv")
            .Should()
            .Equal("/exports", "/exports/daily");
    }
}
