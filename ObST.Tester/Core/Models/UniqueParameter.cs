using NJsonSchema;

namespace ObST.Tester.Core.Models;

class UniqueParameter : PropertyConstraint<UniqueParameter>
{
    /// <summary>
    /// Is this parameter used at least once as resourceIdentifier
    /// </summary>
    public bool IsInResourceIdentifier { get; set; }
    public JsonSchema Schema { get; }
    public ParameterType ParameterType { get; set; }

    public List<UniqueParameter> OtherRelations { get; set; } = new List<UniqueParameter>();

    public UniqueParameter(string mapping, JsonSchema schema) : base(mapping)
    { 
        Schema = schema;
    }

    public override string ToString()
    {
        var origin = !IsRequired ? "" : ParameterType == ParameterType.SelfReferenceCreate ? "*" : ParameterType == ParameterType.SelfReferenceUpsert ? "^" : "!";

        if (!Parents.Any())
        {
            var otherRel = OtherRelations.Any() ? "~[" + string.Join(", ", OtherRelations.Select(r => r.Mapping)) + "]" : "";
            return Mapping + origin + otherRel;
        }
        else
            return "(" + string.Join(";", Parents.Values) + ")>" + Mapping + origin;
    }
}

enum ParameterType
{
    ResourceRepresentation = 0,
    Reference = 0b0001,
    SelfReference = 0b0011,
    SelfReferenceCreate = 0b0111,
    SelfReferenceUpsert = 0b1111,
}

