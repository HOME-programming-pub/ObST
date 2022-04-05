using ObST.Tester.Core.Models;

namespace ObST.Tester.Core.Interfaces;

interface ISutConnector
{
    Task<(HttpResponseMessage response, string body)> RunAsync(SutOperation operation, SutOperationValues parameters, string correlationId);

    Task<bool> ResetAsync();
}
