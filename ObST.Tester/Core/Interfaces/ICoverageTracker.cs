using ObST.Core.Models;
using ObST.Tester.Core.Models;

namespace ObST.Tester.Core.Interfaces;

interface ICoverageTracker
{
    Task AddTrackingAsync(HttpResponseMessage response, SutOperation operation);

    IEnumerable<CoverageResult> CalculateCoverage(Dictionary<string, OperationConfigurations> operations);
}
