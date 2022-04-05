using Microsoft.OpenApi.Models;

namespace ObST.Tester.Core.Models;

record SutParameter
{

    public string Name { get; }
    public string Mapping { get; }
    public ParameterLocation Location { get; }
    public NJsonSchema.JsonSchema Schema { get; }
    public bool Required { get; }

    public SutParameter(string name, string mapping, ParameterLocation location, 
        NJsonSchema.JsonSchema schema, bool required)
    {
        Name = name;
        Mapping = mapping;
        Location = location;
        Schema = schema;
        Required = required;
    }
}
