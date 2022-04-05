namespace ObST.Tester.Core.Models;

class SutRequestBody
{
    public bool Required { get; }
    public IDictionary<string, NJsonSchema.JsonSchema> Content { get; }

    public SutRequestBody(IDictionary<string, NJsonSchema.JsonSchema> content, bool required)
    {
        Content = content;
        Required = required;
    }
}
