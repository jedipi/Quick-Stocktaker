using FluentAssertions;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Services;

namespace QuickStockTaker.UnitTest;

public class StocktakeOperationsServiceTests
{
    [Fact]
    public async Task DeleteBayAsync_UsesCurrentDeleteByBaySql()
    {
        var repository = new TestStocktakeItemRepository([]);
        var service = new StocktakeOperationsService(repository);

        await service.DeleteBayAsync("BAY-1");

        repository.ExecutedCommands.Should().ContainSingle();
        repository.ExecutedCommands[0].Query.Should().Be("DELETE FROM StocktakeItem WHERE BayLocation = ?");
        repository.ExecutedCommands[0].Arguments.Should().Equal("BAY-1");
    }

    [Fact]
    public async Task RenameBayAsync_UsesCurrentBayRenameSqlAndArgumentOrder()
    {
        var repository = new TestStocktakeItemRepository([]);
        var service = new StocktakeOperationsService(repository);

        await service.RenameBayAsync("BAY-1", "BAY-2");

        repository.ExecutedCommands.Should().ContainSingle();
        repository.ExecutedCommands[0].Query.Should().Be("UPDATE StocktakeItem SET BayLocation = ? Where BayLocation = ? ");
        repository.ExecutedCommands[0].Arguments.Should().Equal("BAY-2", "BAY-1");
    }

    [Fact]
    public async Task UpdateItemAsync_UsesCurrentItemUpdateSqlAndArgumentOrder()
    {
        var repository = new TestStocktakeItemRepository([]);
        var service = new StocktakeOperationsService(repository);

        await service.UpdateItemAsync(new StocktakeItem
        {
            Id = 7,
            Barcode = "ABC123",
            Qty = 5
        });

        repository.ExecutedCommands.Should().ContainSingle();
        repository.ExecutedCommands[0].Query.Should().Be("UPDATE StocktakeItem SET Barcode= ?, Qty= ? Where Id= ? ");
        repository.ExecutedCommands[0].Arguments.Should().Equal("ABC123", 5L, 7);
    }

    [Theory]
    [InlineData("stocktake", "ST-200", "UPDATE StocktakeItem SET StocktakeNumber=?")]
    [InlineData("site", "WH-B", "UPDATE StocktakeItem SET Site=?")]
    [InlineData("date", "1/06/2026", "UPDATE StocktakeItem SET StocktakeDate=?")]
    public async Task MetadataUpdates_UseCurrentSql(string field, string value, string expectedSql)
    {
        var repository = new TestStocktakeItemRepository([]);
        var service = new StocktakeOperationsService(repository);

        switch (field)
        {
            case "stocktake":
                await service.UpdateStocktakeNumberAsync(value);
                break;
            case "site":
                await service.UpdateSiteAsync(value);
                break;
            case "date":
                await service.UpdateStocktakeDateAsync(value);
                break;
        }

        repository.ExecutedCommands.Should().ContainSingle();
        repository.ExecutedCommands[0].Query.Should().Be(expectedSql);
        repository.ExecutedCommands[0].Arguments.Should().Equal(value);
    }
}
