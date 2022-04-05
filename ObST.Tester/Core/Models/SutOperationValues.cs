namespace ObST.Tester.Core.Models;

class SutOperationValues
{
    /// <summary>
    /// Dictionary of all used unique parameters with value
    /// </summary>
    private readonly Dictionary<string, (object? value, List<string> usedIn)> _uniqueParameters;

    /// <summary>
    /// Parameters that are appended to the URL.
    /// </summary>
    public Dictionary<string, string> Query { get; }
    /// <summary>
    /// Custom headers that are expected as part of the request.
    /// </summary>
    public Dictionary<string, string> Header { get; }
    /// <summary>
    /// Used together with Path Templating, where the parameter value is actually part
    /// of the operation's URL
    /// </summary>
    public List<string> Path { get; }
    /// <summary>
    /// Used to pass a specific cookie value to the API.
    /// </summary>
    public Dictionary<string, string> Cookie { get; }
    /// <summary>
    /// The request body
    /// </summary>
    public BodyValue? Body { get; set; }

    public TestParameterGeneratorOption UsedGeneratorOptions { get; }


    public SutIdentity Identity { get; }
    public bool IdentityHasAllPermissions { get; }

    public SutOperationValues(Dictionary<string, object?> uniqueParameters, TestParameterGeneratorOption generatorOptions, SutIdentity identity, bool identityHasAllPermissions)
    {

        UsedGeneratorOptions = generatorOptions;

        _uniqueParameters = uniqueParameters.ToDictionary(p => p.Key, p => (p.Value, new List<string>()));

        Path = new List<string>();
        Query = new Dictionary<string, string>();
        Header = new Dictionary<string, string>();
        Cookie = new Dictionary<string, string>();

        Identity = identity;
        IdentityHasAllPermissions = identityHasAllPermissions;
    }

    public object? UseParameter(string mapping, string location)
    {
        var (value, usedIn) = _uniqueParameters[mapping];

        usedIn.Add(location);

        return value;
    }

    public (object? value, List<string> usedIn) GetUsedParameter(string mapping)
    {
        return _uniqueParameters[mapping];
    }
}

class BodyValue
{
    public object? Content { get; set; }
    public string ContentType { get; }

    public BodyValue(string contentType)
    {
        ContentType = contentType;
    }
}

