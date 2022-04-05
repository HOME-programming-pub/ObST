namespace ObST.Core.Models;

public class TestSetupConfiguration
{
    public string? ResetUri { get; set; }
    public QuickCheckConfiguration? QuickCheck { get; set; }
    public TestGeneratorConfig? Generator { get; set; }
    public TestPropertyConfig? Properties { get; set; }
    public List<IdentityConfiguration>? IdentityConfiguration { get; set; }
}
