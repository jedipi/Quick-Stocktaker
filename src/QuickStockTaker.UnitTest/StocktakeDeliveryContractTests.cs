using FluentAssertions;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeDeliveryContractTests
{
    [Fact]
    public void ActiveDeliveryContracts_ExcludeLegacyStatefulExportAndUploaderTypes()
    {
        var coreAssembly = typeof(StocktakeDeliveryWorkflow).Assembly;

        typeof(ICsvExportService).GetInterfaces().Should().NotContain(typeof(IDataExport));
        typeof(CsvExportService).GetProperty("ExportedFile").Should().BeNull();
        coreAssembly.GetType("QuickStockTaker.Core.Services.DataExportFactory").Should().BeNull();
        coreAssembly.GetType("QuickStockTaker.Core.Services.EmailUploadService").Should().BeNull();
        coreAssembly.GetType("QuickStockTaker.Core.Services.FtpUplodService").Should().BeNull();
        coreAssembly.GetType("QuickStockTaker.Core.Services.Interfaces.IEmailUploadService").Should().BeNull();
        coreAssembly.GetType("QuickStockTaker.Core.Services.Interfaces.IFtpUplodService").Should().BeNull();
    }
}
