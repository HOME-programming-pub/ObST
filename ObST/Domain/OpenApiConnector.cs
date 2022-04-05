using ObST.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace ObST.Domain;

public class OpenApiConnector : IOpenApiConnector
{
    private readonly ILogger _logger;

    public OpenApiConnector(ILogger<OpenApiConnector> logger)
    {
        _logger = logger;
    }

    public async Task<OpenApiDocument> RequestAsync(string documentUri)
    {
        _logger.LogInformation("Requesting OpenApiDocument from {OpenApiUri}...", documentUri);

        var uri = new UriBuilder(documentUri).Uri;

        OpenApiDocument openApiDocument;
        OpenApiDiagnostic diagnostic;

        switch (uri.Scheme)
        {
            case "file":
                using (var stream = File.OpenRead(uri.LocalPath))
                    openApiDocument = new OpenApiStreamReader().Read(stream, out diagnostic);
                break;
            case "http":
            case "https":
                using (var client = new HttpClient())
                {
                    var stream = await client.GetStreamAsync(uri);
                    openApiDocument = new OpenApiStreamReader().Read(stream, out diagnostic);
                }
                break;
            default:
                throw new ArgumentException($"Unsupported scheme {uri.Scheme}");

        }


        _logger.LogInformation("Found {title}-{version} ({spec_version})", openApiDocument.Info?.Title, openApiDocument.Info?.Version, diagnostic.SpecificationVersion);

        foreach (var e in diagnostic.Errors)
            _logger.LogWarning("{error}", e);

        if (openApiDocument.Servers?.Any() == false)
        {
            openApiDocument.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer
                    {
                        Description = "Fallback added form Swagger url",
                        Url = uri.Scheme + Uri.SchemeDelimiter + uri.Host + ":" + uri.Port
                    }
                };
        }

        return openApiDocument;
    }
}
