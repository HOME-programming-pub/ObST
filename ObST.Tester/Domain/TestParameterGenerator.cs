using ObST.Core.Models;
using ObST.Tester.Core.Interfaces;
using ObST.Tester.Core.Models;
using FsCheck;
using Microsoft.OpenApi.Models;
using NJsonSchema;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ObST.Tester.Domain;

class TestParameterGenerator : ITestParameterGenerator
{
    private const string SIMPLE_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private readonly ITestConfigurationProvider _testConfigurationProvider;

    public TestParameterGenerator(ITestConfigurationProvider testConfigurationProvider)
    {
        _testConfigurationProvider = testConfigurationProvider;
    }

    public Gen<SutOperationValues> Build(SutOperation operation, TestModel model, IList<TestParameterGeneratorOption> options, IList<SutIdentity> identities)
    {
        var config = _testConfigurationProvider.TestConfiguration?.Setup?.Generator ?? new TestGeneratorConfig();

        var uniqueParams = GenUniqueParameters(operation, model, options, config, identities);

        var gen = uniqueParams
            .SelectMany(values => GenParams(operation.Parameters, values, config)
                .Select(parameters => (parameters, values))
        );

        if (operation.RequestBody != null)
        {
            gen = gen.SelectMany(res => Gen.Elements(operation.RequestBody.Content.Keys.ToArray())
                .SelectMany(key => GenBody(operation.RequestBody.Content[key], key, operation.RequestBody.Required, res.values, config))
                    .Select(body =>
                    {
                        res.parameters.Body = body;
                        return res;
                    }));
        }

        return gen.Select(res => res.parameters);
    }

    private Gen<SutOperationValues> GenUniqueParameters(SutOperation operation, TestModel model, IList<TestParameterGeneratorOption> options, TestGeneratorConfig config, IList<SutIdentity> identities)
    {
        var uniqueParameters = operation.UniqueParameters;

        //create a new dictionary for all unique parameters and select a specific idOption
        var resGen = Gen.Fresh(() => new Dictionary<string, object?>())
           .SelectMany(dict => Gen.Elements(options.ToArray())
               .Select(genOption => (dict, genOption)));

        foreach (var param in uniqueParameters)
        {
            resGen = resGen.SelectMany(values =>
            {
                var (generatorMode, constant) = values.genOption[param.Mapping];

                return GetGenerator(values.dict, generatorMode, constant, param, model, config).Select(value =>
                {
                    values.dict.Add(param.Mapping, value);
                    return values;
                });
            });
        }

        Gen<SutIdentity> identityGen;

        if (operation.ValidIdentities.Any())
        {
            var invalidIdentities = identities.Except(operation.ValidIdentities).ToList();

            if (invalidIdentities.Any())
            {
                identityGen = Gen.Frequency(
                    Tuple.Create(100 - config.UseInvalidOrNullIdentityFrequency, Gen.Elements<SutIdentity>(operation.ValidIdentities)),
                    Tuple.Create(config.UseInvalidOrNullIdentityFrequency, Gen.Elements<SutIdentity>(invalidIdentities)));
            }
            else
                identityGen = Gen.Elements<SutIdentity>(operation.ValidIdentities);
        }
        else
            identityGen = Gen.Elements<SutIdentity>(identities);

        return resGen.SelectMany(values =>
            identityGen.Select(identity => new SutOperationValues(values.dict, values.genOption, identity, operation.ValidIdentities.Contains(identity)))
            );
    }

    private Gen<SutOperationValues> GenParams(IList<SutParameter> parameters, SutOperationValues values, TestGeneratorConfig config)
    {
        var resGen = Gen.Constant(values);

        foreach (var p in parameters)
        {
            var newGen = resGen
                .SelectMany(res => ApplyValues(p.Schema, values, p.Location.ToString(), config)
                    .Select(value =>
                    {
                        if (value is null && !p.Schema.Type.HasFlag(JsonObjectType.Null))
                            return res;

                        var sValue = Convert.ToString(value, CultureInfo.InvariantCulture)!;

                        switch (p.Location)
                        {
                            case ParameterLocation.Query:
                                res.Query.Add(p.Name, sValue);
                                break;
                            case ParameterLocation.Header:
                                res.Header.Add(p.Name, sValue);
                                break;
                            case ParameterLocation.Path:
                                res.Path.Add(sValue);
                                break;
                            case ParameterLocation.Cookie:
                                res.Cookie.Add(p.Name, sValue);
                                break;
                        }

                        return res;
                    })
                );

            if (p.Required)
                resGen = newGen;
            else
                resGen = Gen.Frequency(
                    Tuple.Create(100 - config.IgnoreOptionalPropertiesFrequency, newGen),
                    Tuple.Create(config.IgnoreOptionalPropertiesFrequency, resGen));
        }

        return resGen;
    }

    private Gen<BodyValue> GenBody(JsonSchema schema, string mediaType, bool required, SutOperationValues values, TestGeneratorConfig config)
    {
        var bodyGen = Gen.Fresh(() => new BodyValue(mediaType))
            .SelectMany(res => ApplyValues(schema, values, "body", config)
                .Select(bodyObj =>
                {
                    res.Content = bodyObj;
                    return res;
                })
            );

        if (required)
            return bodyGen;
        else
            return Gen.Frequency(
                Tuple.Create(100 - config.IgnoreOptionalPropertiesFrequency, bodyGen),
                Tuple.Create(config.IgnoreOptionalPropertiesFrequency, Gen.Fresh(() => new BodyValue(mediaType))));


    }

    private Gen<object?> ApplyValues(JsonSchema schema, SutOperationValues values, string parameterLocation, TestGeneratorConfig config)
    {
        if (schema.IsObject)
        {
            var objGen = Gen.Fresh(() => new Dictionary<string, object?>());

            foreach (var p in schema.Properties)
            {
                var pGen = ApplyValues(p.Value, values, parameterLocation, config);

                var newGen = objGen.SelectMany(res =>
                    pGen.Select(value =>
                    {
                        if (value == null && !p.Value.Type.HasFlag(JsonObjectType.Null))
                            return res;

                        res.Add(p.Key, value);
                        return res;
                    }));

                if (schema.RequiredProperties.Contains(p.Key))
                    objGen = newGen;
                else
                    objGen = Gen.Frequency(
                        Tuple.Create(100 - config.IgnoreOptionalPropertiesFrequency, newGen),
                        Tuple.Create(config.IgnoreOptionalPropertiesFrequency, objGen));
            }

            return objGen.Select(o => (object?)o);
        }
        else if (schema.IsArray)
        {
            var itemGen = ApplyValues(schema.Item, values, parameterLocation, config);

            return itemGen.SelectMany(value => Gen.Fresh(() => new List<object?> { value })).Select(o => (object?)o);
        }
        else
            return Gen.Constant(values.UseParameter(schema.Title, parameterLocation)).Select(v => ConvertType(v, schema.Type));
    }

    private Gen<object?> GetGenerator(Dictionary<string, object?> otherValues, GeneratorMode generatorMode, object? constant, UniqueParameter parameter, TestModel model, TestGeneratorConfig config)
    {
        Gen<object?> generator;

        var otherIds = Enumerable.Empty<(string, string)>();

        var p = parameter;

        while (p.Parents.Any())
        {
            p = p.Parents.FirstOrDefault().Value;

            var value = Convert.ToString(otherValues[p.Mapping], CultureInfo.InvariantCulture)!;

            otherIds = otherIds.Prepend((p.Mapping, value));
        }

        IEnumerable<object> knownValues;

        switch (generatorMode)
        {
            case GeneratorMode.Random:
                generator = GetRandomValueGenerator(parameter.Schema);

                knownValues = model.Get(otherIds, parameter.Schema.Title, includeDeleted: true);

                if (knownValues.Any())
                {
                    var existingIdGen = Gen.Elements<object?>(knownValues.ToArray());

                    generator = Gen.Frequency(
                        Tuple.Create(100 - config.UseKnownIdFrequency, generator),
                        Tuple.Create(config.UseKnownIdFrequency, existingIdGen)
                        );
                }
                break;
            case GeneratorMode.RequireUnknown:
                generator = GetRandomValueGenerator(parameter.Schema);

                knownValues = model.Get(otherIds, parameter.Schema.Title, includeDeleted: true);

                generator = generator.Where(o => knownValues.All(k => !k.Equals(o)));
                break;
            case GeneratorMode.UseConstant:
                generator = Gen.Constant(constant);
                break;
            default:
                throw new InvalidOperationException($"Unknown GeneratorMode: {generatorMode}");
        }

        if (parameter.Schema.Type.HasFlag(JsonObjectType.Null))
            generator = Gen.Frequency(
                Tuple.Create(100 - config.NullValueForNullableFrequency, generator),
                Tuple.Create(config.NullValueForNullableFrequency, Gen.Constant<object?>(null))
                );

        return generator;
    }

    private Gen<object?> GetRandomValueGenerator(JsonSchema schema)
    {
        if (schema.IsEnumeration)
            return Gen.Elements<object?>(schema.Enumeration);

        return (schema.Type & ~JsonObjectType.Null) switch
        {
            JsonObjectType.Boolean => Arb.Generate<bool>().Select(b => (object?)b),
            JsonObjectType.Integer => schema.Format switch
            {
                "int16" => Gen.Choose(
                    Convert.ToInt32(schema.Minimum.GetValueOrDefault(short.MinValue)),
                    Convert.ToInt32(schema.Maximum.GetValueOrDefault(short.MaxValue))
                    ).Select(i => (object?)i),
                null => Gen.Choose(
                    Convert.ToInt32(schema.Minimum.GetValueOrDefault(int.MinValue)),
                    Convert.ToInt32(schema.Maximum.GetValueOrDefault(int.MaxValue))
                    ).Select(i => (object?)i),
                "int32" => Gen.Choose(
                    Convert.ToInt32(schema.Minimum.GetValueOrDefault(int.MinValue)),
                    Convert.ToInt32(schema.Maximum.GetValueOrDefault(int.MaxValue))
                    ).Select(i => (object?)i),
                //Uses the int32 generator
                "int64" => Gen.Choose(
                    Convert.ToInt32(schema.Minimum.GetValueOrDefault(int.MinValue)),
                    Convert.ToInt32(schema.Maximum.GetValueOrDefault(int.MaxValue))
                    ).Select(i => (object?)i),
                _ => throw new NotImplementedException($"Unknown format {nameof(OpenApiSchema)}.{nameof(OpenApiSchema.Format)} for type {nameof(OpenApiSchema)}.{nameof(OpenApiSchema.Type)}!"),
            },
            JsonObjectType.String => schema.Format switch
            {
                "date-time" => Arb.Default.DateTime().Generator.Select(d => (object?)d.ToString("o", CultureInfo.InvariantCulture)),
                "date" => Arb.Default.DateTime().Generator.Select(d => (object?)d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                "time" => Arb.Default.DateTime().Generator.Select(d => (object?)d.ToString("HH:mm:ss.fffffffK", CultureInfo.InvariantCulture)),
                _ => Gen.Choose(schema.MinLength.GetValueOrDefault(0), schema.MaxLength.GetValueOrDefault(1000)).SelectMany(l => Gen.ArrayOf(l, Gen.Elements<char>(SIMPLE_CHARS))).Select(o => (object?)new string(o)), //Arb.Default.StringWithoutNullChars().Generator.Select(s => (object)s.Get).Where(s => s != null),
            },
            _ => throw new NotImplementedException($"Unknown {nameof(OpenApiSchema)}.{nameof(OpenApiSchema.Type)} {schema.Type}!"),
        };
    }

    private object? ConvertType(object? value, JsonObjectType type)
    {
        //convert ids to the requiered body type
        if (value is string s)
            return (type & ~JsonObjectType.Null) switch
            {
                JsonObjectType.Integer => long.Parse(s),
                JsonObjectType.String => s,
                JsonObjectType.Boolean => Convert.ToBoolean(s),
                _ => throw new NotImplementedException($"Cannot convert string to type '{type}'"),
            };
        else
            return value;
    }

    private IEnumerable<string> GetIdPrecedors(string key, IEnumerable<string> otherIds)
    {
        foreach (var otherId in otherIds)
        {
            if (key == otherId)
                break;

            if (key.StartsWith(otherId[..^3]))
                yield return otherId;
        }
    }
}
