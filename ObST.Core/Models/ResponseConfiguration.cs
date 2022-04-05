namespace ObST.Core.Models;

public class ResponseConfiguration
{
    /// <summary>
    /// Placeholder for headers. Not fully implemented
    /// </summary>
    public List<string>? Headers { get; set; }

    public Dictionary<string, ContentConfiguration>? Content { get; set; }
}
