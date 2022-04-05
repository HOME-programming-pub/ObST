using ObST.Core.Models;
using ObST.Core.Util;
using ObST.Tester.Core.Models;
using KellermanSoftware.CompareNetObjects;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace ObST.Tester.Domain.Util;

static class TestConfigurationExtention
{


    public static IList<(string mapping, string value)> GetIdsOfPath(this TestConfiguration configuration, string path, bool caseInsensitive)
    {
        var res = new List<(string, string)>();

        var currentPath = configuration.Paths;

        if (currentPath is null)
            return res;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var s in segments)
        {
            object? match;

            if (caseInsensitive)
                match = currentPath.FirstOrDefault(p => p.Key.ToLowerInvariant() == s.ToLowerInvariant()).Value;
            else
                match = currentPath.GetValueOrDefault(s);

            if (match is null)
            {
                string key;
                (key, match) = currentPath.FirstOrDefault(p => p.Key.StartsWith("{"));

                if (match is not null)
                    res.Add((key[1..^1], s));
                else
                    throw new ArgumentException("Input path does not match any existing path");
            }

            currentPath = (PathConfigurations)match;
        }

        return res;
    }

    public static IList<SutOperation> ToSutOperations(this TestConfiguration config, IList<SutIdentity> identities)
    {
        var res = new List<SutOperation>();

        if (config.Operations is not null)
            foreach (var path in config.Operations)
            {
                foreach (var op in path.Value)
                {
                    var operationId = op.Value.OperationId ?? throw new ArgumentException($"{nameof(OperationConfiguration.OperationId)} muss be set for all operations!");
                    var doesCreate = op.Value.Responses?.ContainsKey("201") == true;

                    var pathParameterMappings = config.GetIdsOfPath(path.Key, false).Select(p => p.mapping).ToList();

                    var (requestBody, bodyParameters) = ToSutRequestBody(op.Value.RequestBody, config.ResourceClasses ?? new ResourceClassConfigurations());

                    var (parameters, uniqueParameters) = ToSutParameters(
                            op.Value.Parameters ?? new ParameterConfigurations(),
                            pathParameterMappings,
                            config.Parameters?.GetValueOrDefault(operationId) ?? new ParameterMappingConfiguration(),
                            bodyParameters,
                            op.Key,
                            doesCreate
                            );

                    res.Add(new SutOperation(
                        operationId,
                        op.Key,
                        path.Key,
                        op.Value.Servers?.Where(s => s.Url is not null).Select(s => s.Url!).ToList() ?? throw new ArgumentException($"{nameof(OperationConfiguration.Servers)} muss be set for all operations!"),
                        parameters,
                        uniqueParameters,
                        requestBody,
                        ToSutResponses(op.Value.Responses!, config.ResourceClasses ?? new ResourceClassConfigurations()),
                        doesCreate,
                        FilterValidIdentities(op.Value.Security, identities)));
                }
            }

        return res;
    }

    private static (IList<SutParameter> parameters, IList<UniqueParameter> uniqueParameters) ToSutParameters(
        ParameterConfigurations parameters,
        IList<string> pathParameterMappings,
        ParameterMappingConfiguration mappings,
        Dictionary<string, UniqueParameter> bodyParameters,
        OperationType operationType,
        bool doesCreate)
    {
        if (operationType == OperationType.Post)
        {
            foreach (var selfReference in bodyParameters.Values.Where(p => p.ParameterType.HasFlag(ParameterType.SelfReference)))
                selfReference.ParameterType |= ParameterType.SelfReferenceCreate;
        }

        var resParams = new List<SutParameter>();
        var uniqueParameters = bodyParameters;
        var parentsToAdd = new Dictionary<string, string[]>();

        var allParameters =
            (parameters.Path ?? Enumerable.Empty<ParameterConfiguration>()).Select(p => (p, (string?)null, ParameterLocation.Path))!
            .Concat((parameters.Query ?? Enumerable.Empty<KeyValuePair<string, ParameterConfiguration>>()).Select(p => (p.Value, p.Key, ParameterLocation.Query)))
            .Concat((parameters.Header ?? Enumerable.Empty<KeyValuePair<string, ParameterConfiguration>>()).Select(p => (p.Value, p.Key, ParameterLocation.Header)))
            .Concat((parameters.Cookie ?? Enumerable.Empty<KeyValuePair<string, ParameterConfiguration>>()).Select(p => (p.Value, p.Key, ParameterLocation.Cookie)))
            .ToList<(ParameterConfiguration parameter, string name, ParameterLocation location)>();

        var pathParamCount = 0;

        foreach (var (parameter, name, location) in allParameters)
        {
            var mapping = location switch
            {
                ParameterLocation.Path => pathParameterMappings[pathParamCount++],
                ParameterLocation.Query => mappings.Query![name],
                ParameterLocation.Header => mappings.Header![name],
                ParameterLocation.Cookie => mappings.Cookie![name],
                _ => throw new ArgumentException("Unknown parameter Location")
            };

            if (mapping == null)
                throw new ArgumentException($"Unknown mapping for parameter {name} in {location}");

            var parents = mapping.Split('<');
            mapping = parents.Last();

            if (parents.Length > 1)
            {
                if (!parentsToAdd.TryGetValue(mapping, out var match))
                {
                    parentsToAdd.Add(mapping, parents);
                }
                else
                {
                    if (match.Length > parents.Length)
                    {
                        for (int i = 1; i <= parents.Length; i++)
                            if (parents[^i] != match[^i])
                                throw new ArgumentException($"Different parents {parents[^i]} and {match[^i]} specified for parameter {mapping}");
                    }
                    else
                    {
                        for (int i = 1; i <= match.Length; i++)
                            if (parents[^i] != match[^i])
                                throw new ArgumentException($"Different parents {parents[^i]} and {match[^i]} specified for parameter {mapping}");

                        parentsToAdd[mapping] = parents;
                    }
                }
            }

            parameter.Schema!.Title = mapping;

            var schema = ToNJsonSchema(parameter.Schema);

            resParams.Add(new SutParameter(
                name,
                mapping,
                location,
                schema,
                location == ParameterLocation.Path || (parameter.Required ?? false)
                ));

            if (uniqueParameters.TryGetValue(mapping, out var uniqueParam))
                uniqueParam.Schema.ShouldCompare(schema);
            else
            {
                uniqueParam = new UniqueParameter(mapping, schema);
                uniqueParameters.Add(mapping, uniqueParam);

                if (mapping.IsIdMapping())
                    uniqueParam.ParameterType = ParameterType.Reference;
            }

            if (location == ParameterLocation.Path || (parameter.Required ?? false) && !(parameter.Schema.Nullable ?? false))
                uniqueParam.IsRequired = true;

            if (location == ParameterLocation.Path || location == ParameterLocation.Query)
                uniqueParam.IsInResourceIdentifier = true;

            if (operationType == OperationType.Put && doesCreate &&
                pathParameterMappings.LastOrDefault() == mapping)
                uniqueParam.ParameterType |= ParameterType.SelfReferenceUpsert;
        }

        foreach (var e in parentsToAdd.Values)
        {
            for (int i = 0; i < e.Length - 1; i++)
            {
                if (!uniqueParameters.TryGetValue(e[i], out var p1))
                    throw new ArgumentException($"Specified parent parameter {e[i]} not found!");

                if (!uniqueParameters.TryGetValue(e[i + 1], out var p2))
                    throw new ArgumentException($"Specified parent parameter {e[i + 1]} not found!");

                p1.LinkChild(p2);
            }
        }


        return (resParams, uniqueParameters.Values.OrderBy(p => p, UniqueParameterComparer.Instance).ToList());
    }

    private static (SutRequestBody? body, Dictionary<string, UniqueParameter> parameters) ToSutRequestBody(RequestBodyConfiguration? body, ResourceClassConfigurations resourceClasses)
    {
        if (body is null)
            return (null, new Dictionary<string, UniqueParameter>());

        var content = new Dictionary<string, NJsonSchema.JsonSchema>();
        var uniqueParameters = new Dictionary<string, UniqueParameter>();

        if (body.Content is not null)
            foreach (var c in body.Content)
            {
                var schema = FilterSchema(c.Value.Schema!, true);

                if(schema is not null)
                {
                    var parameters = GetBodyParameters(schema, string.Empty, ParameterType.ResourceRepresentation, resourceClasses);

                    foreach (var p in parameters)
                    {
                        uniqueParameters.TryAdd(p.Mapping, p);
                    }

                    content.Add(c.Key, ToNJsonSchema(schema));
                }
            }

        return (new SutRequestBody(content, body.Required), uniqueParameters);
    }

    private static IEnumerable<UniqueParameter> GetBodyParameters(JsonSchemaConfiguration schema, string mapping, ParameterType parameterType, ResourceClassConfigurations resourceClasses)
    {
        if (schema.Type == "object")
        {
            var res = Enumerable.Empty<UniqueParameter>();


            if (schema.ResourceClass == null)
                throw new ArgumentException("ResourceClass not specified for object");

            var resourceClass = resourceClasses.First(r => r.Value == schema.ResourceClass);

            schema.Title = string.IsNullOrEmpty(mapping) ? resourceClass.Key : mapping;

            if (schema.Properties == null)
                return res;

            foreach (var p in schema.Properties)
            {
                var pMapping = resourceClass.Value[p.Key];

                if (pMapping.StartsWith(":"))
                {
                    parameterType = pMapping == IdMappingExtention.ID_MAPPING ? ParameterType.SelfReference : ParameterType.ResourceRepresentation;
                    pMapping = resourceClass.Key + pMapping;
                }
                else
                {
                    parameterType = ParameterType.Reference;
                }

                res = res.Concat(GetBodyParameters(p.Value, pMapping, parameterType, resourceClasses));
            }

            return res;
        }
        else if (schema.Type == "array")
        {
            return GetBodyParameters(schema.Items!, mapping.Replace("[]", ""), parameterType, resourceClasses);
        }
        else
        {
            schema.Title = mapping;
            return Enumerable.Repeat(new UniqueParameter(mapping, ToNJsonSchema(schema))
            {
                IsInResourceIdentifier = false,
                ParameterType = parameterType
            }, 1);
        }
    }

    private static Dictionary<string, SutResponse> ToSutResponses(Dictionary<string, ResponseConfiguration> responses, ResourceClassConfigurations resourceClasses)
    {
        return responses.ToDictionary(r => r.Key, r =>
        {
            var content = r.Value.Content?.SingleOrDefault();

            JsonSchemaConfiguration? schema;

            if (content?.Value.Schema != null)
            {
                schema = FilterSchema(content.Value.Value.Schema, false);
                if (schema is not null)
                    GetBodyParameters(schema, string.Empty, ParameterType.ResourceRepresentation, resourceClasses);
            }
            else
                schema = null;


            return new SutResponse(content?.Key, schema is not null ? ToNJsonSchema(schema) : null);
        });
    }

    private static NJsonSchema.JsonSchema ToNJsonSchema(JsonSchemaConfiguration schema)
    {
        var json = JsonConvert.SerializeObject(schema, new JsonSchemaConfigurationConverter());

        var res = NJsonSchema.JsonSchema.FromJsonAsync(json).GetAwaiter().GetResult();

        return res;
    }

    private static JsonSchemaConfiguration? FilterSchema(JsonSchemaConfiguration schema, bool isRequest)
    {
        if (isRequest && schema.ReadOnly == true || !isRequest && schema.WriteOnly == true)
            return null;

        if (schema.AllOf != null)
        {
            if (isRequest && schema.AllOf.Any(s => s.ReadOnly == true) || !isRequest && schema.AllOf.Any(s => s.WriteOnly == true))
                return null;

            var merged = new JsonSchemaConfiguration
            {
                ResourceClass = schema.ResourceClass,
            };

            foreach (var s in schema.AllOf)
            {
                if (s.AdditionalPropertiesAllowed != null)
                    merged.AdditionalPropertiesAllowed = s.AdditionalPropertiesAllowed;
                if (s.Enum != null)
                    merged.Enum = (merged.Enum ?? Enumerable.Empty<string>()).Concat(s.Enum).ToHashSet().ToList();
                if (s.Format != null)
                    merged.Format = s.Format;
                if (s.Maximum != null)
                    merged.Maximum = s.Maximum;
                if (s.MaxLength != null)
                    merged.MaxLength = s.MaxLength;
                if (s.Minimum != null)
                    merged.Minimum = s.Minimum;
                if (s.MinLength != null)
                    merged.MinLength = s.MinLength;
                if (s.Nullable != null)
                    merged.Nullable = s.Nullable;
                if (s.Properties != null)
                    merged.Properties = (merged.Properties ?? new Dictionary<string, JsonSchemaConfiguration>())
                        .Where(p => !s.Properties.ContainsKey(p.Key))
                        .Concat(s.Properties)
                        .ToDictionary(p => p.Key, p => p.Value);
                if (s.ReadOnly != null)
                    merged.ReadOnly = s.ReadOnly;
                if (s.Required != null)
                    merged.Required = s.Required;
                if (s.Title != null)
                    merged.Title = s.Title;
                if (s.ResourceClass != null)
                    merged.ResourceClass = s.ResourceClass;
                if (s.Type != null)
                    merged.Type = s.Type;
                if (s.WriteOnly != null)
                    merged.WriteOnly = s.WriteOnly;
            }

            return FilterSchema(merged, isRequest);
        }

        JsonSchemaConfiguration? items;

        if (schema.Items != null)
        {
            items = FilterSchema(schema.Items, isRequest);
            if (items is not null)
                items.Title = schema.Title;
        }
        else
            items = null;

        return new JsonSchemaConfiguration
        {
            AdditionalPropertiesAllowed = schema.AdditionalPropertiesAllowed,
            AllOf = schema.AllOf?.Select(s => FilterSchema(s, isRequest)).Where(s => s is not null).ToList()!,
            Enum = schema.Enum,
            Format = schema.Format,
            Items = items,
            Maximum = schema.Maximum,
            Minimum = schema.Minimum,
            Properties = schema.Properties?.ToDictionary(s => s.Key, s => FilterSchema(s.Value, isRequest)).Where(s => s.Value is not null).ToDictionary(s => s.Key, s => s.Value!),
            MaxLength = schema.MaxLength,
            MinLength = schema.MinLength,
            Nullable = schema.Nullable,
            ReadOnly = schema.ReadOnly,
            WriteOnly = schema.WriteOnly,
            Required = schema.Required,
            ResourceClass = schema.ResourceClass,
            Title = schema.Title,
            Type = schema.Type
        };
    }

    private static ISet<SutIdentity> FilterValidIdentities(IList<SecurityRequirementsConfiguration>? requirements, IList<SutIdentity> identities)
    {
        if (requirements is null || !requirements.Any())
            return identities.ToHashSet();

        var res = new HashSet<SutIdentity>();

        foreach (var r in requirements)
        {
            //no security requirements
            if (!r.Any())
                return identities.ToHashSet();

            if (r.Count != 1)
                throw new NotSupportedException("Compound security requirements are not supported");

            foreach (var i in identities)
            {
                if (i == SutIdentity.NULL_IDENTITY)
                    continue;

                if (i.SecuritySchemeName == r.First().Key && !r.First().Value.Except(i.Scopes).Any())
                    res.Add(i);
            }
        }

        return res;
    }


    private class UniqueParameterComparer : IComparer<UniqueParameter>
    {
        public static UniqueParameterComparer Instance = new();

        public int Compare(UniqueParameter? x, UniqueParameter? y)
        {
            if (!x!.Parents.Any())
                return !y!.Parents.Any() ? 0 : -1;
            else if (!y!.Parents.Any())
                return 1;

            return Compare(x.Parents.First().Value, y.Parents.First().Value);
        }
    }

    public static IList<SutIdentity> ToSutIndentities(this TestConfiguration config)
    {
        return (config.Setup?.IdentityConfiguration ?? Enumerable.Empty<IdentityConfiguration>())
            .Select(c =>
            {
                var scheme = config.SecuritySchemes!.First(s => s.Key == c.SecurityScheme);

                return new SutIdentity(scheme.Key + Guid.NewGuid().ToString(), scheme.Key, scheme.Value, c.Scopes ?? new List<string>())
                {
                    ClientId = c.ClientId,
                    ClientSecret = c.ClientSecret,
                    ApiKey = c.ApiKey
                };
            })
            .Append(SutIdentity.NULL_IDENTITY)
            .ToList();
    }
}
