namespace ObST.Core.Models;
public class ParameterMappingConfiguration
{
    public Dictionary<string, string>? Query { get; set; }
    public Dictionary<string, string>? Header { get; set; }
    public Dictionary<string, string>? Cookie { get; set; }
}
