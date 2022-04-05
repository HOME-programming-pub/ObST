namespace ObST.Core.Models;

public class TestGeneratorConfig
{
    /// <summary>
    /// Frequency how often optional properties should be ignored 
    /// </summary>
    public int IgnoreOptionalPropertiesFrequency { get; set; }

    /// <summary>
    /// Frequency how often nullable properties are set to null
    /// </summary>
    public int NullValueForNullableFrequency { get; set; }

    /// <summary>
    /// Frequency how often a id form the list of known ids should be used instead of generating a new one
    /// </summary>
    public int UseKnownIdFrequency { get; set; }

    /// <summary>
    /// Frequency how often an invalid or null identity should be used, when a valid identity is required
    /// </summary>
    public int UseInvalidOrNullIdentityFrequency { get; set; }
}
