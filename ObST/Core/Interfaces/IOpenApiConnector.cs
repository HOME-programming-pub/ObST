using Microsoft.OpenApi.Models;

namespace ObST.Core.Interfaces;

/// <summary>
/// Requests and parses the OpenApi document
/// </summary>
public interface IOpenApiConnector
{
    Task<OpenApiDocument> RequestAsync(string uri);
}
