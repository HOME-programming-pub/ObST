using Microsoft.OpenApi.Models;

namespace ObST.Tester.Core.Models;

class SutOperation
{
    public string OperationId { get; }
    public OperationType OperationType { get; }
    public string Path { get; }
    public IList<string> ServerUrls { get; }

    public IList<SutParameter> Parameters { get; }
    public IList<UniqueParameter> UniqueParameters { get; }

    public SutRequestBody? RequestBody { get; }

    public IDictionary<string, SutResponse> Responses { get; }

    public bool DoesCreate { get; }

    public ISet<SutIdentity> ValidIdentities { get; }

    public SutOperation(string operationId, OperationType type, string path, IList<string> serverUrls, 
        IList<SutParameter> parameters, IList<UniqueParameter> uniqueParameters, SutRequestBody? requestBody, 
        IDictionary<string, SutResponse> responses, bool doesCreate, ISet<SutIdentity> validIdentities)
    {
        OperationId = operationId;
        OperationType = type;
        Path = path;
        ServerUrls = serverUrls;
        Parameters = parameters;
        UniqueParameters = uniqueParameters;
        RequestBody = requestBody;
        Responses = responses;
        DoesCreate = doesCreate;
        ValidIdentities = validIdentities;
    }

    public IList<UniqueParameter> GetNeeds()
    {
        return UniqueParameters.Where(p => p.ParameterType == ParameterType.Reference || p.ParameterType == ParameterType.SelfReference).ToList();
    }
}
