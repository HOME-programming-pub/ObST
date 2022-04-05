using Microsoft.OpenApi.Models;

namespace ObST.Analyzer.Core.Models;

class AnalyzedPath
{
    public ResourcePath Path { get; }
    public List<ResourcePathParameter> Parameters { get; }
    public bool LastSegmentIsParameter { get; }
    public OpenApiPathItem PathItem { get; }

    public AnalyzedPath(ResourcePath path, List<ResourcePathParameter> parameters, OpenApiPathItem pathItem, bool lastSegmentIsParameter)
    {
        Path = path;
        Parameters = parameters;
        LastSegmentIsParameter = lastSegmentIsParameter;
        PathItem = pathItem;
    }
}
