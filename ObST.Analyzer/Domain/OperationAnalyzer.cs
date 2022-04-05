using ObST.Analyzer.Core.Models;
using ObST.Domain.OasAnalyzer;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace ObST.Analyzer.Domain;

class OperationAnalyzer
{
    private readonly SchemaAnalyzer _schemaAnalyzer;
    private readonly ILogger _logger;

    public OperationAnalyzer(SchemaAnalyzer schemaAnalyzer, ILogger logger)
    {
        _logger = logger;
        _schemaAnalyzer = schemaAnalyzer;
    }


    public IEnumerable<AnalyzedOperation> AnalyzePathOperations(AnalyzedPath path, IList<OpenApiSecurityRequirement> documentSecurity)
    {
        foreach (var o in path.PathItem.Operations)
            yield return AnalyzeOperation(o.Key, o.Value, path, documentSecurity);
    }

    private AnalyzedOperation AnalyzeOperation(OperationType type, OpenApiOperation item, AnalyzedPath path, IList<OpenApiSecurityRequirement> documentSecurity)
    {
        new ResponsesAnalyzer(_schemaAnalyzer, _logger).Analyze(item.Responses, type, path.Path);

        var op = new OpenApiOperation
        {
            OperationId = string.IsNullOrEmpty(item.OperationId) ? $"{type} {path.Path.Path}" : item.OperationId,
            //override document security
            Security = item.Security.Any() ? item.Security : documentSecurity,
            Servers = item.Servers.Any() ? item.Servers : path.Path.Servers,
            Parameters = new ParametersAnalyzer(_schemaAnalyzer).Analyze(path, item),

            //basic operation scoped fields
            Deprecated = item.Deprecated,
            Callbacks = item.Callbacks,
            Responses = item.Responses,
            RequestBody = item.RequestBody,
            ExternalDocs = item.ExternalDocs,
            Description = item.Description,
            Summary = item.Summary,
            Tags = item.Tags,
            Extensions = item.Extensions
        };

        return new AnalyzedOperation(path, type, op);
    }
}
