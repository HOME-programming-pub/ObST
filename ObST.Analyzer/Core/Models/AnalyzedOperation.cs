using Microsoft.OpenApi.Models;

namespace ObST.Analyzer.Core.Models;

class AnalyzedOperation
{
    public AnalyzedPath Path { get; }
    public OperationType OperationType { get; }
    public OpenApiOperation Operation { get; }

    public AnalyzedOperation(AnalyzedPath path, OperationType type, OpenApiOperation operation)
    {
        Path = path;
        OperationType = type;
        Operation = operation;
    }
}

