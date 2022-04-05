using ObST.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ObST.Tester.Domain.Util;
class JsonSchemaConfigurationConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(JsonSchemaConfiguration);
    }

    public override bool CanRead => false;
    public override bool CanWrite => true;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var schema = (JsonSchemaConfiguration)value;

        var res = new JObject();

        if (schema.Title != null)
            res.Add("title", schema.Title);

        if (schema.Type != null)
            if (schema.Nullable == true)
                res.Add("type", JArray.FromObject(new[] { schema.Type, "null" }));
            else
                res.Add("type", schema.Type);

        if (schema.Format != null)
            res.Add("format", schema.Format);

        if (schema.Required != null)
            res.Add("required", JArray.FromObject(schema.Required));

        if (schema.Items != null)
            res.Add("items", JObject.FromObject(schema.Items, serializer));

        if (schema.Properties != null)
            res.Add("properties", JObject.FromObject(schema.Properties, serializer));

        if (schema.AdditionalPropertiesAllowed != null)
            res.Add("additionalProperties", schema.AdditionalPropertiesAllowed);

        if (schema.Enum != null)
            res.Add("enum", JArray.FromObject(schema.Enum));

        if (schema.Maximum != null)
            res.Add("maximum", schema.Maximum);

        if (schema.Minimum != null)
            res.Add("minimum", schema.Minimum);

        if (schema.MaxLength != null)
            res.Add("maxLength", schema.MaxLength);

        if (schema.MinLength != null)
            res.Add("minLength", schema.MinLength);

        if (schema.ReadOnly != null)
            res.Add("readOnly", schema.ReadOnly);

        if (schema.WriteOnly != null)
            res.Add("writeOnly", schema.WriteOnly);

        res.WriteTo(writer);
    }
}
