using ObST.Core.Models;
using ObST.Tester.Core.Interfaces;
using ObST.Tester.Domain;
using FsCheck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ObST.Tester;

public class Startup
{

    public static void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<ITestConfigurationProvider, TestConfigurationProvider>()
            .AddSingleton<ISutConnector, SutConnector>()
            .AddSingleton<ITestParameterGenerator, TestParameterGenerator>()
            .AddSingleton<ICoverageTracker, CoverageTracker>()
            .AddSingleton<IIdentityConnector, IdentityConnector>()
            .AddSingleton<TestSpec>();
    }

    public static void Run(TestConfiguration configuration, IServiceProvider sp)
    {
        sp.GetRequiredService<ITestConfigurationProvider>().TestConfiguration = configuration;

        var logger = sp.GetRequiredService<ILogger<Startup>>();

        var pathCount = configuration.Operations?.Count ?? 0;
        var opCount = configuration.Operations?.SelectMany(p => p.Value).Count() ?? 0;

        if(pathCount == 0 || opCount == 0)
        {
            logger.LogError("At least one path and operation must be specified!");
            return;
        }

        logger.LogInformation("The Test Configuration specifies {pathCount} pathes with a total of {operationCount} operations", pathCount, opCount);

        var spec = sp.GetRequiredService<TestSpec>();

        var quickConfig = configuration?.Setup?.QuickCheck ?? new();
        var quickCheckConfig = new Configuration
        {
            MaxNbOfTest = quickConfig.MaxNbOfTest,
            StartSize = quickConfig.StartSize,
            EndSize = quickConfig.EndSize
        };

        if (quickConfig.Seed is not null)
            quickCheckConfig.Replay = Tuple.Create(quickConfig.Seed.Seed1, quickConfig.Seed.Seed2, quickConfig.Seed.Size);

        try
        {
            spec.ToPropertyWithoutModelDryRun().Check(quickCheckConfig);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error when checking SUT!");
        }

        var coverage = sp.GetRequiredService<ICoverageTracker>().CalculateCoverage(configuration!.Operations!);

        foreach (var c in coverage)
        {
            logger.LogInformation("{operationId} Covered: {covered} - Not covered: {notCovered} - Not documented: {undocumented}",
                c.OperationId,
                string.Join(", ", c.CoveredStatusCodes),
                string.Join(", ", c.AllStatusCodes.Except(c.CoveredStatusCodes)),
                string.Join(", ", c.NotDocumentedStatusCodes));
        }

    }
}

