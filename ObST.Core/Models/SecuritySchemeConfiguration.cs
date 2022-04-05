using Microsoft.OpenApi.Models;

namespace ObST.Core.Models;

public class SecuritySchemeConfiguration
{
    public SecuritySchemeType Type { get; set; }

    /// <summary>
    /// Name of the parameter to use. Applies To <see cref="Type"/>="apiKey"
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Location of the parameter to use. Applies To <see cref="Type"/>="apiKey"
    /// </summary>
    public string? In { get; set; }

    /// <summary>
    /// Url to discover OAuth2 configuration values. Applies To <see cref="Type"/>="openIdConnect"
    /// </summary>
    public string? OpenIdConnectUrl { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is SecuritySchemeConfiguration other)
            return Equals(other);
        else
            return false;
    }

    public bool Equals(SecuritySchemeConfiguration other)
    {
        return Type == other.Type &&
            Name == other.Name &&
            In == other.In &&
            OpenIdConnectUrl == other.OpenIdConnectUrl;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Name, In, OpenIdConnectUrl);
    }
}
