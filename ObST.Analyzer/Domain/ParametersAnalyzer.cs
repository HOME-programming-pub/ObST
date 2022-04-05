using ObST.Analyzer.Core.Models;
using ObST.Domain.OasAnalyzer;
using Microsoft.OpenApi.Models;

namespace ObST.Analyzer.Domain;

internal class ParametersAnalyzer
{
    private readonly SchemaAnalyzer _schemaAnalyzer;
    private static int unknownCounter;

    public ParametersAnalyzer(SchemaAnalyzer schemaAnalyzer)
    {
        _schemaAnalyzer = schemaAnalyzer;
    }

    public IList<OpenApiParameter> Analyze(AnalyzedPath path, OpenApiOperation operation)
    {
        AnalyzeAndMapRequestBody(operation.RequestBody);

        //override path parameters
        var parameters = AnalyzerUtil.MergePathAndOperationParameters(path.PathItem.Parameters, operation.Parameters);


        var list = new List<OpenApiParameter>();

        var lastPathParameter = path.LastSegmentIsParameter ? path.Parameters.Last() : null;

        foreach (var p in path.Parameters)
            list.Add(p.Parameter!);

        foreach (var p in parameters.Where(p => p.In != ParameterLocation.Path))
        {
            if (p.Schema.Type != "object")
                p.Schema.Title = "UNKNOWN_" + unknownCounter++;

            list.Add(p);
        }

        return list;
    }

    private void AnalyzeAndMapRequestBody(OpenApiRequestBody body)
    {
        if (body == null)
            return;

        var firstContent = body.Content.Values.First();

        _schemaAnalyzer.AnalyzeAndMapSchema(firstContent.Schema);

        var analyzed = firstContent.Schema;

        if (body.Content.Any(c => c.Value.Schema != analyzed))
            throw new ArgumentException("Schemas do not match for all content types of this request!");
    }

}
