using YamlDotNet.Serialization;

namespace ObST.Core.Models;
public class PathConfigurations : Dictionary<string, object>
{

    public PathConfigurations() : base() { }
    public PathConfigurations(IDictionary<string, object> dict) : base(dict) { }

    [YamlIgnore]
    public string? Type
    {
        get => (string?)this.GetValueOrDefault("$type");
        set => this["$type"] = value ?? string.Empty;
    }
}
