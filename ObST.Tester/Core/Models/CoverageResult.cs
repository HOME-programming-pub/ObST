namespace ObST.Core.Models;

public class CoverageResult
{

    public string OperationId { get; }
    public ISet<string> AllStatusCodes { get; }
    public ISet<string> CoveredStatusCodes { get; }
    public ISet<string> NotDocumentedStatusCodes { get; }

    public CoverageResult(string operationId, ISet<string> all, ISet<string> covered, ISet<string> notDocumented)
    {
        OperationId = operationId;
        AllStatusCodes = all;
        CoveredStatusCodes = covered;
        NotDocumentedStatusCodes = notDocumented;
    }
}
