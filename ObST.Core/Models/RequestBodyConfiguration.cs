namespace ObST.Core.Models;

public class RequestBodyConfiguration
{
    public bool Required { get; set; }
    public Dictionary<string, ContentConfiguration>? Content { get; set; }
}
