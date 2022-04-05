using ObST.Analyzer.Core.Models;
using ObST.Core.Models;
using ObST.Domain.OasAnalyzer;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace ObST.Analyzer.Domain;

public class OasAnalyzer
{
    private readonly OpenApiDocument _document;
    private readonly ILogger _logger;

    private readonly bool _ignoreCaseInPathNames;

    private readonly SchemaAnalyzer _schemaAnalyzer;

    private readonly Dictionary<OpenApiSchema, JsonSchemaConfiguration> _convertedSchemas = new();

    public OasAnalyzer(OpenApiDocument document, string idPattern, string primaryResourceIdPattern, bool ignoreCaseInPathNames, ILogger<OasAnalyzer> logger)
    {
        _document = document;
        _logger = logger;
        _ignoreCaseInPathNames = ignoreCaseInPathNames;

        _schemaAnalyzer = new SchemaAnalyzer(idPattern, primaryResourceIdPattern, _document.Components, logger);
    }

    public TestConfiguration Build()
    {
        var paths = PathsAnalyzer.ExtractPaths(_document);

        var operationAnalyzer = new OperationAnalyzer(_schemaAnalyzer, _logger);

        var operations = paths.SelectMany(p => operationAnalyzer.AnalyzePathOperations(p, _document.SecurityRequirements)).ToList();

        var resourceClasses = new ResourceClassConfigurations();

        foreach (var r in _schemaAnalyzer.ResourceClasses)
        {
            var properties = new ResourceClassConfiguration();

            foreach (var p in r.Value.Properties)
                properties.Add(p.Key, p.Value.Title);

            resourceClasses.Add(r.Key.ToString(), properties);
        }

        var servers = new HashSet<ServerConfiguration>();

        foreach (var s in operations.SelectMany(op => op.Operation.Servers))
        {
            servers.Add(new ServerConfiguration
            {
                Url = s.Url,
                Description = s.Description
            });
        }

        var securitySchemes = new Dictionary<string, (SecuritySchemeConfiguration Scheme, ISet<string> Scopes)>();

        foreach (var s in _document.Components.SecuritySchemes)
        {
            securitySchemes.Add(s.Key, (BuildSecuritySchemeConfiguration(s.Value), new HashSet<string>()));
        }

        //must be run before BuildIdentityConfigurations()
        var opConfigs = BuildOperationConfiguration(operations, securitySchemes, servers, resourceClasses);

        return new TestConfiguration
        {
            Setup = new TestSetupConfiguration
            {
                ResetUri = "PLEASE SET MANUALLY",
                QuickCheck = new QuickCheckConfiguration
                {
                    MaxNbOfTest = 100,
                    StartSize = 2,
                    EndSize = 5
                },
                Generator = new TestGeneratorConfig
                {
                    //TODO change from % to pro 1 
                    IgnoreOptionalPropertiesFrequency = 10,
                    NullValueForNullableFrequency = 10,
                    UseKnownIdFrequency = 95,
                    UseInvalidOrNullIdentityFrequency = 5
                },
                Properties = new TestPropertyConfig
                {
                    NoBadRequestWhenValidDataIsProvided = false,
                    ResponseDocumentation = false
                },
                IdentityConfiguration = BuildIdentityConfigurations(securitySchemes)
            },
            Servers = servers,
            ResourceClasses = resourceClasses,
            SecuritySchemes = securitySchemes.Any() ? securitySchemes.ToDictionary(s => s.Key, s => s.Value.Scheme) : null,
            Paths = BuildPathConfiguration(paths),
            Parameters = BuildParameterMappingConfiguration(operations),
            Operations = opConfigs
        };
    }

    private PathConfigurations BuildPathConfiguration(IList<AnalyzedPath> paths)
    {
        var config = new PathConfigurations();

        foreach (var p in paths)
        {
            var current = config;
            var parents = string.Empty;

            int parameterCount = 0;

            foreach (var seg in p.Path.Path!.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                var mapping = seg;

                if (seg.StartsWith('{'))
                {
                    mapping = "{" + parents + p.Parameters[parameterCount].Mapping + "}";
                    parents += p.Parameters[parameterCount].Mapping + "<";
                    parameterCount++;
                }

                if (!current.TryGetValue(mapping, out var match))
                {
                    match = new PathConfigurations();
                    current.Add(mapping, match);
                }

                current = (PathConfigurations)match;
            }

            current.Type = p.Path.ResourceClass?.ToString();
        }

        return config;
    }

    private Dictionary<string, ParameterMappingConfiguration> BuildParameterMappingConfiguration(IList<AnalyzedOperation> operations)
    {
        var res = new Dictionary<string, ParameterMappingConfiguration>();

        foreach (var op in operations)
        {
            var parameters = op.Operation.Parameters.Where(p => p.In != ParameterLocation.Path);

            if (!parameters.Any())
                continue;

            var config = new ParameterMappingConfiguration();

            foreach (var p in parameters)
            {
                switch (p.In)
                {
                    case ParameterLocation.Query:
                        config.Query ??= new Dictionary<string, string>();
                        config.Query.Add(p.Name, p.Schema.Title);
                        break;
                    case ParameterLocation.Header:
                        config.Header ??= new Dictionary<string, string>();
                        config.Header.Add(p.Name, p.Schema.Title);
                        break;
                    case ParameterLocation.Cookie:
                        config.Cookie ??= new Dictionary<string, string>();
                        config.Cookie.Add(p.Name, p.Schema.Title);
                        break;
                }
            }

            res.Add(op.Operation.OperationId, config);
        }

        return res;
    }

    private Dictionary<string, OperationConfigurations> BuildOperationConfiguration(
        IList<AnalyzedOperation> operations, 
        Dictionary<string, (SecuritySchemeConfiguration Scheme, ISet<string> Scopes)> securitySchemes,
        HashSet<ServerConfiguration> servers,
        ResourceClassConfigurations resourceClasses)
    {
        var res = new Dictionary<string, OperationConfigurations>();

        foreach (var pathGroup in operations.GroupBy(op => op.Path.Path.Path!))
        {
            var ops = new OperationConfigurations();

            foreach (var op in pathGroup)
            {
                var s = op.Operation.Servers.Select(s => new ServerConfiguration { Url = s.Url, Description = s.Description });

                ops.Add(op.OperationType, new OperationConfiguration
                {
                    OperationId = op.Operation.OperationId,
                    Parameters = BuildParametersConfigurations(op.Path.Path, op.Operation.Parameters, resourceClasses),
                    RequestBody = op.Operation.RequestBody != null ? BuildRequestBodyConfiguration(op.Operation.RequestBody, resourceClasses) : null,
                    Responses = new ResponseConfigurations(op.Operation.Responses.ToDictionary(r => r.Key, r => BuildResponseConfiguration(r.Value, resourceClasses))),
                    Servers = servers.Where(o => s.Contains(o)).ToList(),
                    Security = BuildSecurityRequirementsConfiguration(op.Operation.Security, securitySchemes)
                });
            }

            res.Add(pathGroup.Key, ops);
        }

        return res;
    }

    private ParameterConfigurations BuildParametersConfigurations(ResourcePath path, IList<OpenApiParameter> parameters, ResourceClassConfigurations resourceClasses)
    {
        var res = new ParameterConfigurations
        {
            Path = new List<ParameterConfiguration>(),
            Query = new Dictionary<string, ParameterConfiguration>(),
            Header = new Dictionary<string, ParameterConfiguration>(),
            Cookie = new Dictionary<string, ParameterConfiguration>()
        };

        var orderedPathParameters = path.Parameters!.Join(parameters.Where(p => p.In == ParameterLocation.Path), p => p.name, p => p.Name, (p1, p2) => p2);

        foreach (var p in orderedPathParameters)
        {
            res.Path.Add(new ParameterConfiguration
            {
                Schema = BuildJsonSchemaConfiguration(p.Schema, resourceClasses)
            });
        }

        foreach (var p in parameters)
        {
            Action<string, ParameterConfiguration> addAction;

            switch (p.In)
            {
                case ParameterLocation.Path:
                    continue;
                case ParameterLocation.Query:
                    addAction = res.Query.Add;
                    break;
                case ParameterLocation.Header:
                    addAction = res.Header.Add;
                    break;
                case ParameterLocation.Cookie:
                    addAction = res.Cookie.Add;
                    break;
                default:
                    throw new InvalidOperationException($"Missing location {nameof(OpenApiParameter.In)} for parameter '{p.Name}'");
            }

            addAction(p.Name, new ParameterConfiguration
            {
                Required = p.Required,
                Schema = BuildJsonSchemaConfiguration(p.Schema, resourceClasses)
            });
        }

        if (!res.Path.Any())
            res.Path = null;

        if (!res.Query.Any())
            res.Query = null;

        if (!res.Header.Any())
            res.Header = null;

        if (!res.Cookie.Any())
            res.Cookie = null;

        return res;
    }

    private RequestBodyConfiguration BuildRequestBodyConfiguration(OpenApiRequestBody body, ResourceClassConfigurations resourceClasses)
    {
        return new RequestBodyConfiguration
        {
            Required = body.Required,
            Content = body.Content.ToDictionary(c => c.Key, c => BuildContentConfiguration(c.Value, resourceClasses))
        };
    }

    private ResponseConfiguration BuildResponseConfiguration(OpenApiResponse response, ResourceClassConfigurations resourceClasses)
    {
        return new ResponseConfiguration
        {
            Headers = response.Headers?.Any() == true ? response.Headers.Keys.ToList() : null,
            Content = response.Content?.Any() == true ? response.Content.ToDictionary(c => c.Key, c => BuildContentConfiguration(c.Value, resourceClasses)) : null
        };
    }

    private ContentConfiguration BuildContentConfiguration(OpenApiMediaType mediaType, ResourceClassConfigurations resourceClasses)
    {
        return new ContentConfiguration
        {
            Schema = BuildJsonSchemaConfiguration(mediaType.Schema, resourceClasses)
        };
    }

    private JsonSchemaConfiguration BuildJsonSchemaConfiguration(OpenApiSchema schema, ResourceClassConfigurations resourceClasses)
    {
        if (_convertedSchemas.TryGetValue(schema, out var match))
            return match;

        var res = new JsonSchemaConfiguration
        {
            AllOf = schema.AllOf.Any() ? schema.AllOf.Select(s => BuildJsonSchemaConfiguration(s, resourceClasses)).ToList() : null,
            AdditionalPropertiesAllowed = schema.Type == "object" ? schema.AdditionalPropertiesAllowed : null,
            Format = schema.Format,
            Items = schema.Items != null ? BuildJsonSchemaConfiguration(schema.Items, resourceClasses) : null,
            Maximum = schema.Maximum,
            Minimum = schema.Minimum,
            MaxLength = schema.MaxLength,
            MinLength = schema.MinLength,
            Nullable = schema.Nullable ? true : (bool?)null,
            Properties = schema.Properties.Any() ? schema.Properties.ToDictionary(p => p.Key, p => BuildJsonSchemaConfiguration(p.Value, resourceClasses)) : null,
            Required = schema.Required.Any() ? schema.Required.ToList() : null,
            Type = schema.Type,
            Enum = schema.Enum.Any() && schema.Type == "string" ? schema.Enum.Select(e => ((OpenApiString)e).Value).ToList() : null,
            ResourceClass = resourceClasses.FirstOrDefault(r => r.Key == schema.Title).Value,
            ReadOnly = schema.ReadOnly ? true : (bool?)null,
            WriteOnly = schema.WriteOnly ? true : (bool?)null,
        };

        _convertedSchemas.Add(schema, res);

        return res;
    }

    private List<SecurityRequirementsConfiguration>? BuildSecurityRequirementsConfiguration(IList<OpenApiSecurityRequirement> requirements, Dictionary<string, (SecuritySchemeConfiguration Scheme, ISet<string> Scopes)> securitySchemes)
    {
        var res = new List<SecurityRequirementsConfiguration>();

        foreach (var r in requirements)
        {
            var req = new SecurityRequirementsConfiguration();

            foreach (var e in r)
            {
                var scheme = BuildSecuritySchemeConfiguration(e.Key);
                var kvp = securitySchemes.First(s => s.Value.Scheme.Equals(scheme));

                foreach (var scope in e.Value)
                    kvp.Value.Scopes.Add(scope);

                req.Add(kvp.Key, e.Value.ToArray());
            }

            res.Add(req);
        }

        return res.Any() ? res : null;
    }

    private SecuritySchemeConfiguration BuildSecuritySchemeConfiguration(OpenApiSecurityScheme scheme)
    {
        return new SecuritySchemeConfiguration
        {
            Type = scheme.Type,
            In = scheme.Type == SecuritySchemeType.ApiKey ? scheme.In.ToString() : null,
            Name = scheme.Type == SecuritySchemeType.ApiKey ? scheme.Name : null,
            OpenIdConnectUrl = scheme.Type == SecuritySchemeType.OpenIdConnect ? scheme.OpenIdConnectUrl?.ToString() : null
        };
    }

    private List<IdentityConfiguration>? BuildIdentityConfigurations(Dictionary<string, (SecuritySchemeConfiguration Scheme, ISet<string> Scopes)> securitySchemes)
    {
        var res = new List<IdentityConfiguration>();

        foreach (var s in securitySchemes)
        {
            res.Add(new IdentityConfiguration
            {
                SecurityScheme = s.Key,
                ApiKey = s.Value.Scheme.Type == SecuritySchemeType.ApiKey ? "API_KEY" : null,
                Scopes = s.Value.Scheme.Type == SecuritySchemeType.OAuth2 || s.Value.Scheme.Type == SecuritySchemeType.OpenIdConnect ? s.Value.Scopes.ToList() : null,
                ClientId = s.Value.Scheme.Type == SecuritySchemeType.OAuth2 || s.Value.Scheme.Type == SecuritySchemeType.OpenIdConnect ? "CLIENT_ID" : null,
                ClientSecret = s.Value.Scheme.Type == SecuritySchemeType.OAuth2 || s.Value.Scheme.Type == SecuritySchemeType.OpenIdConnect ? "CLIENT_SECRET" : null,
            });
        }

        return res.Any() ? res : null;
    }

}
