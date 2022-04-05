
namespace ObST.Core.Models;

public class TestConfiguration
{
    public TestSetupConfiguration? Setup { get; set; }
    public HashSet<ServerConfiguration>? Servers { get; set; }
    public ResourceClassConfigurations? ResourceClasses { get; set; }
    public PathConfigurations? Paths { get; set; }
    public Dictionary<string, ParameterMappingConfiguration>? Parameters { get; set; }

    public Dictionary<string, SecuritySchemeConfiguration>? SecuritySchemes { get; set; }

    public Dictionary<string, OperationConfigurations>? Operations { get; set; }
}
