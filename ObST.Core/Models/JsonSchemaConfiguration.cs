using YamlDotNet.Serialization;

namespace ObST.Core.Models;

public class JsonSchemaConfiguration
{
    [YamlIgnore]
    public string? Title { get; set; }
    public ResourceClassConfiguration? ResourceClass { get; set; }
    public List<JsonSchemaConfiguration>? AllOf { get; set; }
    public string? Type { get; set; }
    public string? Format { get; set; }
    public List<string>? Required { get; set; }
    public JsonSchemaConfiguration? Items { get; set; }
    public Dictionary<string, JsonSchemaConfiguration>? Properties { get; set; }
    public bool? AdditionalPropertiesAllowed { get; set; }
    public List<string>? Enum { get; set; }
    public bool? Nullable { get; set; }
    public decimal? Maximum { get; set; }
    public decimal? Minimum { get; set; }
    public int? MaxLength { get; set; }
    public int? MinLength { get; set; }
    public bool? ReadOnly { get; set; }
    public bool? WriteOnly { get; set; }
}
