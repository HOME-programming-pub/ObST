namespace ObST.Core.Models;

public class IdentityConfiguration
{
    public string? SecurityScheme { get; set; }
    public string? ApiKey { get; set; }
    public List<string>? Scopes { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

