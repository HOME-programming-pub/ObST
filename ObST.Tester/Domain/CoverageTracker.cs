using ObST.Core.Models;
using ObST.Tester.Core.Interfaces;
using ObST.Tester.Core.Models;
using System.Collections.Concurrent;

namespace ObST.Tester.Domain;

class CoverageTracker : ICoverageTracker
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _coverage = new ConcurrentDictionary<string, HashSet<string>>();

    public Task AddTrackingAsync(HttpResponseMessage response, SutOperation operation)
    {

        var set = new HashSet<string>
            {
                ((int)response.StatusCode).ToString()
            };

        _coverage.AddOrUpdate(operation.OperationId, set, (key, value) => { value.Add(((int)response.StatusCode).ToString()); return value; });

        return Task.CompletedTask;
    }

    public IEnumerable<CoverageResult> CalculateCoverage(Dictionary<string, OperationConfigurations> operations)
    {
        foreach (var o in operations.Values.SelectMany(op => op.Values))
        {
            if (o.Responses is null || o.OperationId is null)
                continue;

            var allCodes = o.Responses.Select(r => r.Key).ToHashSet();

            if (!_coverage.TryGetValue(o.OperationId, out var covered))
            {
                yield return new CoverageResult(o.OperationId, allCodes, new HashSet<string>(), new HashSet<string>());
            }
            else
            {
                yield return new CoverageResult(o.OperationId, allCodes, allCodes.Intersect(covered).ToHashSet(), covered.Except(allCodes).ToHashSet());
            }
        }
    }
}
