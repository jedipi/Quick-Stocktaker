using Autofac;
using FluentAssertions;
using NSubstitute;
using QuickStockTaker.Core.Data;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeRemoteConnectionServiceTests
{
    [Fact]
    public void CoreServiceRegistration_ResolvesConnectionServiceThroughItsContract()
    {
        var builder = new ContainerBuilder();
        builder.RegisterAssemblyTypes(typeof(Constants).Assembly)
            .Where(type => type.Name.EndsWith("Service"))
            .AsImplementedInterfaces();
        builder.RegisterAssemblyTypes(typeof(Constants).Assembly)
            .Where(type => type.Name.EndsWith("Validator"));
        builder.RegisterInstance(new TestAppPreferences()).As<IAppPreferences>();
        builder.RegisterInstance(Substitute.For<ISecureStorageService>());
        using var container = builder.Build();

        container.Resolve<IStocktakeRemoteConnectionService>().Should().NotBeNull();
    }

    [Fact]
    public async Task TestConnectionAsync_WhenFtpIsConfigured_UsesCapturedFtpConfiguration()
    {
        var preferences = new TestAppPreferences();
        preferences.Set(Constants.FtpUseSftp, false);
        preferences.Set(Constants.FtpHost, " ftp.example.com ");
        preferences.Set(Constants.FtpPort, "2121");
        preferences.Set(Constants.FtpFolder, "/exports");
        var secureStorage = Substitute.For<ISecureStorageService>();
        secureStorage.GetAsync(Constants.FtpUsername).Returns("stocktaker");
        secureStorage.GetAsync(Constants.FtpPassword).Returns("secret");
        StocktakeRemoteConfiguration? capturedConfiguration = null;
        var service = new StocktakeRemoteConnectionService(
            preferences,
            secureStorage,
            new StocktakeRemoteConfigurationValidator(),
            (configuration, _) =>
            {
                capturedConfiguration = configuration;
                return Task.CompletedTask;
            },
            (_, _) => throw new InvalidOperationException("SFTP should not be selected."));

        var result = await service.TestConnectionAsync(TestContext.Current.CancellationToken);

        result.Should().Be((true, "Connection successful."));
        capturedConfiguration.Should().Be(new StocktakeRemoteConfiguration(
            StocktakeRemoteProtocol.Ftp,
            "ftp.example.com",
            2121,
            "/exports",
            "stocktaker",
            "secret"));
    }
}
