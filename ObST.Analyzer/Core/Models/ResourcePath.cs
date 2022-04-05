using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace ObST.Analyzer.Core.Models;

class ResourcePath
{
    /// <summary>
    /// The resource class
    /// </summary>
    public ResourceClass? ResourceClass { get; set; }

    /// <summary>
    /// Request Path.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///  An optional, string summary, intended to apply to all operations in this path.
    /// </summary>
    public string? PathSummery { get; set; }

    /// <summary>
    ///  An optional, string description, intended to apply to all operations in this path.
    /// </summary>
    public string? PathDescription { get; set; }

    /// <summary>
    /// This object MAY be extended with Specification Extensions for the path.
    /// </summary>
    public IDictionary<string, IOpenApiExtension>? PathExtensions { get; set; }

    public IList<OperationType>? KnownMethods { get; set; }

    /// <summary>
    /// List of path parameters. Order matches the hierarchical path order
    /// </summary>
    public List<(string name, string? mapping)>? Parameters { get; set; }

    public IList<OpenApiServer>? Servers { get; set; }

}
