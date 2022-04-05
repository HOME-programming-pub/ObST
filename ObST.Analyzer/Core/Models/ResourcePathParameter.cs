using Microsoft.OpenApi.Models;

namespace ObST.Analyzer.Core.Models;

record ResourcePathParameter
{
    public string Name { get; }
    public string? Mapping { get; set; }

    /// <summary>
    /// origin path containing all segments til this parameter
    /// </summary>
    public string PartialPath { get; }

    public OpenApiParameter? Parameter { get; set; }

    public ResourcePathParameter(string name, string partialPath)
    {
        Name = name;
        PartialPath = partialPath;
    }
}