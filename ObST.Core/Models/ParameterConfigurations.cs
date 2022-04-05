namespace ObST.Core.Models;
public class ParameterConfigurations
{

    public List<ParameterConfiguration>? Path { get; set; }
    public Dictionary<string, ParameterConfiguration>? Query { get; set; }
    public Dictionary<string, ParameterConfiguration>? Header { get; set; }
    public Dictionary<string, ParameterConfiguration>? Cookie { get; set; }
}
