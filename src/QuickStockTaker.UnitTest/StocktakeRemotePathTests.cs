using FluentAssertions;
using QuickStockTaker.Core.Services;

namespace QuickStockTaker.UnitTest;

public class StocktakeRemotePathTests
{
    [Theory]
    [InlineData("", "stock.csv", "stock.csv")]
    [InlineData(" exports / daily ", "stock.csv", "exports/daily/stock.csv")]
    [InlineData("/exports/daily", "stock.csv", "/exports/daily/stock.csv")]
    public void BuildRemotePath_JoinsFolderAndFileName(string folder, string fileName, string expected)
    {
        StocktakeRemotePath.Build(folder, fileName, path => path).Should().Be(expected);
    }

    [Fact]
    public void BuildSftpDirectoryPaths_WhenRemotePathIsRelative_ReturnsIncrementalDirectories()
    {
        StocktakeRemotePath.BuildDirectoryPaths("exports/daily/stock.csv")
            .Should()
            .Equal("exports", "exports/daily");
    }

    [Fact]
    public void BuildSftpDirectoryPaths_WhenRemotePathIsRooted_ReturnsRootedIncrementalDirectories()
    {
        StocktakeRemotePath.BuildDirectoryPaths("/exports/daily/stock.csv")
            .Should()
            .Equal("/exports", "/exports/daily");
    }
}
