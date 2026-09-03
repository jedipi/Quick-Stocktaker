using Autofac;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Reflection;
using QuickStockTaker.Core.Services;
using QuickStockTaker.Core.Services.Interfaces;
using QuickStockTaker.Core.Validators;

namespace QuickStockTaker.UnitTest;

public sealed class StocktakeDeliveryWorkflowRegistrationTests
{
    [Fact]
    public void DirectRegistration_ResolvesFullyInitializedProductionWorkflow()
    {
        var builder = new ContainerBuilder();
        builder.RegisterInstance(Substitute.For<ICsvExportService>());
        builder.RegisterInstance(NullLogger<StocktakeDeliveryWorkflow>.Instance)
            .As<ILogger<StocktakeDeliveryWorkflow>>();
        builder.RegisterInstance(new StocktakeDeliveryOperationGate());
        builder.RegisterInstance(Substitute.For<IAppPreferences>());
        builder.RegisterInstance(Substitute.For<ISecureStorageService>());
        builder.RegisterInstance(new StocktakeRemoteConfigurationValidator());
        builder.RegisterInstance(Substitute.For<IStocktakeEmailAdapter>());
        builder.RegisterInstance(new StocktakeEmailConfigurationValidator());
        builder.RegisterInstance(Substitute.For<IStocktakeEmailConfigurationGate>());
        builder.RegisterInstance(Substitute.For<IStocktakeRemoteConfigurationGate>());
        builder.RegisterInstance(Substitute.For<IStocktakeWorkspace>());
        builder.RegisterType<StocktakeDeliveryWorkflow>()
            .FindConstructorsWith(type => type.GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic))
            .As<IStocktakeDeliveryWorkflow>();

        using var container = builder.Build();

        container.Resolve<IStocktakeDeliveryWorkflow>()
            .Should().BeOfType<StocktakeDeliveryWorkflow>();
    }
}
