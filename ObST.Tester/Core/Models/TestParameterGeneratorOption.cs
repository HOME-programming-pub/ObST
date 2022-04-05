namespace ObST.Tester.Core.Models;

class TestParameterGeneratorOption : Dictionary<string, (GeneratorMode mode, object? constant)>
{

    public TestParameterGeneratorOption() : base()
    {

    }

    public TestParameterGeneratorOption(IDictionary<string, (GeneratorMode, object?)> dictionary) : base(dictionary)
    {

    }

    public void Add(string mapping, GeneratorMode mode)
    {
        Add(mapping, (mode, null));
    }
    public void AddConstant(string mapping, object? constant)
    {
        Add(mapping, (GeneratorMode.UseConstant, constant));
    }

    public override bool Equals(object? obj)
    {
        return base.Equals(obj as TestParameterGeneratorOption);
    }

    public bool Equals(TestParameterGeneratorOption other)
    {
        return Count == other.Count &&
            this.All(e =>
                other.TryGetValue(e.Key, out var value) &&
                value.mode == e.Value.mode &&
                Equals(value.constant, e.Value.constant));
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(0);

        foreach (var e in this.OrderBy(e => e.Key))
        {
            hash = HashCode.Combine(hash, e.Key, e.Value.mode, e.Value.constant);
        }

        return hash;
    }

}

enum GeneratorMode
{
    /// <summary>
    /// Including unknown, known and deleted
    /// </summary>
    Random = 0,
    RequireUnknown,
    UseConstant
}
