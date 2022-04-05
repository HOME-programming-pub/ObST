namespace ObST.Core.Models;

public class ServerConfiguration
{
    public string? Url { get; set; }
    public string? Description { get; set; }

    public override int GetHashCode()
    {
        return HashCode.Combine(Url, Description);
    }

    public override bool Equals(object? obj)
    {
        if (obj is ServerConfiguration other)
            return Equals(other);
        else
            return false;
    }

    public bool Equals(ServerConfiguration other)
    {
        return
            Url == other.Url &&
            Description == other.Description;
    }
}
