using ObST.Core.Interfaces;
using ObST.Core.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ObST.Domain;

class TestConfigurationReaderWriter : ITestConfigurationWriter, ITestConfigurationReader
{
    private readonly ILogger _logger;


    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public TestConfigurationReaderWriter(ILogger<TestConfigurationReaderWriter> logger)
    {
        _logger = logger;

        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public async Task<TestConfiguration?> ReadAsync(string path)
    {
        if (!File.Exists(path))
        {
            _logger.LogError("The test configuration file does not exist! At path: {path}", path);
            return null;
        }

        var yaml = await File.ReadAllTextAsync(path);

        var res = _deserializer.Deserialize<TestConfiguration>(yaml);

        if (res.Paths != null)
            CastPathConfigurations(res.Paths);

        return res;
    }

    private void CastPathConfigurations(PathConfigurations path)
    {
        var kvps = path.Where(p => p.Key != "$type").ToList();

        foreach (var p in kvps)
        {
            var v = new PathConfigurations(((IDictionary<object, object>)p.Value).ToDictionary(e => (string)e.Key, e => e.Value));
            path[p.Key] = v;

            CastPathConfigurations(v);
        }
    }

    public async Task WriteAsync(string path, TestConfiguration config)
    {
        var yaml = _serializer.Serialize(config);

        await File.WriteAllTextAsync(path, yaml);
    }
}
