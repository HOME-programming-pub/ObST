namespace ObST.Core.Models;
public class OperationConfiguration
{
    public string? OperationId { get; set; }
    public ParameterConfigurations? Parameters { get; set; }
    public RequestBodyConfiguration? RequestBody { get; set; }
    public ResponseConfigurations? Responses { get; set; }

    public List<ServerConfiguration>? Servers { get; set; }
    public List<SecurityRequirementsConfiguration>? Security { get; set; }
}
