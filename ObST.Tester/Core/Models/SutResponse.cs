namespace ObST.Tester.Core.Models;

record SutResponse
{
    public string? ContentType { get; }
    public NJsonSchema.JsonSchema? Schema { get; }

    public SutResponse(string? contentType, NJsonSchema.JsonSchema? schema)
    {
        ContentType = contentType;
        Schema = schema;
    }
}
