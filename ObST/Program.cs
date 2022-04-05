using ObST.Analyzer.Domain;
using ObST.Core.Interfaces;
using ObST.Domain;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace ObST;

class Program
{
    private abstract class BaseOptions
    {
        [Option("verbose", HelpText = "Set log level to verbose")]
        public bool Verbose { get; set; }
    }

    [Verb("analyze", HelpText = "Analyze an OpenAPI Specification and generate a TestConfiguration")]
    private class AnalyzeOptions : BaseOptions
    {
        [Option('i', "in", Required = true, HelpText = "OpenAPI Specification file to be processed.")]
        public string? InputFile { get; set; }

        [Option('o', "out", HelpText = "Test Configuration output file. Defaults to config.yaml")]
        public string? OutputFile { get; set; } = "config.yaml";

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                        new Example("Generate a test config", new AnalyzeOptions { InputFile = "oas.yaml", OutputFile = null })
                    };
            }
        }
    }

    [Verb("test", HelpText = "Run the test")]
    private class TestOptions : BaseOptions
    {
        [Option('i', "in", Required = false, HelpText = "Input file to be processed.")]
        public string InputFile { get; set; } = "config.yaml";

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                        new Example("Run tests using a test config", new TestOptions { InputFile = "config.yaml" })
                    };
            }
        }
    }

    static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<AnalyzeOptions, TestOptions>(args)
            .MapResult(
            (AnalyzeOptions opts) => RunAnalyze(opts),
            (TestOptions opts) => RunTest(opts),
            errs => -1
            );
    }

    private static IServiceCollection GetDefaultServices(BaseOptions opts)
    {
        var logLevel = opts.Verbose ? LogEventLevel.Verbose : LogEventLevel.Information;

        var serilogger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console()
            .CreateLogger();

        var services = new ServiceCollection()
          .AddLogging(builder =>
          {
              builder.AddSerilog(serilogger, true);
          });

        return services;
    }

    private static int RunAnalyze(AnalyzeOptions opts)
    {
        var services = GetDefaultServices(opts);

        services
            .AddSingleton<IOpenApiConnector, OpenApiConnector>()
            .AddSingleton<ITestConfigurationWriter, TestConfigurationReaderWriter>();

        var sp = services.BuildServiceProvider();

        var idPattern = @"(?i)id$";
        var primaryResourceIdPattern = @"^(?i)id$";

        var document = sp.GetRequiredService<IOpenApiConnector>().RequestAsync(opts.InputFile!).GetAwaiter().GetResult();

        var builder = new OasAnalyzer(document, idPattern, primaryResourceIdPattern, false, sp.GetRequiredService<ILogger<OasAnalyzer>>());

        var configuration = builder.Build();

        sp.GetRequiredService<ITestConfigurationWriter>().WriteAsync(opts.OutputFile!, configuration).GetAwaiter().GetResult();

        var logger = sp.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Successfully generated test configuration {path}!", opts.OutputFile);

        return 0;
    }

    private static int RunTest(TestOptions opts)
    {
        var services = GetDefaultServices(opts);

        services
            .AddSingleton<ITestConfigurationReader, TestConfigurationReaderWriter>();

        Tester.Startup.ConfigureServices(services);
       

        var sp = services.BuildServiceProvider();

        var configuration = sp.GetRequiredService<ITestConfigurationReader>().ReadAsync(opts.InputFile).GetAwaiter().GetResult();

        if (configuration is null)
            return -1;

        Tester.Startup.Run(configuration, sp);

        return 0;
    }
}
