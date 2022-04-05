namespace ObST.Core.Models;

public class TestPropertyConfig
{
    /// <summary>
    /// Check whether the response is documented (StatusCode, ResponseBody)
    /// </summary>
    public bool ResponseDocumentation { get; set; }
    public bool NoBadRequestWhenValidDataIsProvided { get; set; }
}
