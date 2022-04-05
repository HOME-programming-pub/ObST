namespace ObST.Analyzer.Core.Models;

public class ResourceClass
{
    public ResourceClass? Subordinate { get; set; }

    public string? Name { get; set; }


    public override bool Equals(object? obj)
    {
        if (obj is ResourceClass other)
            return Equals(other);
        else
            return false;
    }

    public bool Equals(ResourceClass other)
    {
        return Name == other.Name && Equals(Subordinate, other.Subordinate);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Subordinate);
    }

    public override string ToString()
    {
        if (Subordinate == null)
            return Name ?? "Missing Name!";
        else
            return Subordinate.ToString("[]");
    }

    private string ToString(string partial)
    {
        if (Subordinate == null)
            return Name + partial;
        else
            return Subordinate.ToString("[" + partial + "]");
    }

}