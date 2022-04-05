using Microsoft.OpenApi.Models;

namespace ObST.Analyzer.Domain;

static class AnalyzerUtil
{

    public static IList<OpenApiParameter> MergePathAndOperationParameters(
        IList<OpenApiParameter> pathParameters,
        IList<OpenApiParameter> operationParameters)
    {
        return (pathParameters ?? new List<OpenApiParameter>())
                //remove all params which will be overridden
                .Where(pp => !operationParameters.Any(op => op.Name == pp.Name && op.In == pp.In))
                //concat operation and path params 
                .Concat(operationParameters)
                .ToList();
    }
}
