using ObST.Core.Models;

namespace ObST.Tester.Core.Models;

class SutIdentity
{
    public static SutIdentity NULL_IDENTITY = new("NULL_IDENTITY", "NULL_IDENTITY", new SecuritySchemeConfiguration(), new List<string>())
    {
    };

    public string Id { get; }
    public string SecuritySchemeName { get; }
    public SecuritySchemeConfiguration SecurityScheme { get; }

    public string? ApiKey { get; set; }

    public List<string> Scopes { get; }

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    public SutIdentity(string id, string securitySchemeName, SecuritySchemeConfiguration securityScheme, List<string> scopes)
    {
        Id=id;
        SecuritySchemeName=securitySchemeName;
        SecurityScheme=securityScheme;
        Scopes=scopes;
    }
}

