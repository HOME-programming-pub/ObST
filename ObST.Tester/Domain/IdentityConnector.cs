using ObST.Tester.Core.Interfaces;
using ObST.Tester.Core.Models;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace ObST.Tester.Domain;

class IdentityConnector : IIdentityConnector
{
    private static readonly DateTime EPOCH = new DateTime(1970, 01, 01, 00, 00, 00, DateTimeKind.Utc);

    private readonly ILogger _logger;
    private readonly Dictionary<string, CacheObject> _cache = new Dictionary<string, CacheObject>();

    public IdentityConnector(ILogger<IdentityConnector> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetIdentityInformationAsync(SutIdentity identity)
    {
        switch (identity.SecurityScheme.Type)
        {
            case SecuritySchemeType.ApiKey:
                return identity.ApiKey!;
            case SecuritySchemeType.OpenIdConnect:
                if (_cache.TryGetValue(identity.Id, out var match))
                {
                    if (match.GoodBefore > DateTime.UtcNow)
                        return match.Value;
                }

                var client = new HttpClient();
                var disco = await client.GetDiscoveryDocumentAsync(identity.SecurityScheme.OpenIdConnectUrl);

                if (disco.IsError)
                {
                    _logger.LogError("Failed to request discovery document from {openIdConnectUrl}", identity.SecurityScheme.OpenIdConnectUrl);
                    throw new InvalidOperationException("Failed to request access token");
                }

                var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    ClientId = identity.ClientId,
                    ClientSecret = identity.ClientSecret,
                    Scope = string.Join(" ", identity.Scopes)
                });


                if (tokenResponse.IsError)
                {
                    _logger.LogError("Failed to request token using {grandType} and {clientId}", "client credentials", identity.ClientId);
                    throw new InvalidOperationException("Failed to rquest access token");
                }

                _cache.Add(identity.Id, new CacheObject(EPOCH.AddSeconds(tokenResponse.ExpiresIn), tokenResponse.AccessToken));

                return tokenResponse.AccessToken;
            default:
                throw new NotSupportedException($"Unsupported security scheme: {identity.SecurityScheme.Type}");
        }
    }

    private record CacheObject
    {
        public DateTime GoodBefore { get; }
        public string Value { get; }

        public CacheObject(DateTime goodBefore, string value)
        {
            GoodBefore = goodBefore;
            Value = value;
        }
    }
}
