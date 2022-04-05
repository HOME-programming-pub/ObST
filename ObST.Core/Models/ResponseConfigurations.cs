namespace ObST.Core.Models;

public class ResponseConfigurations : Dictionary<string, ResponseConfiguration>
{
    public ResponseConfigurations() : base()
    {

    }

    public ResponseConfigurations(IDictionary<string, ResponseConfiguration> dictionary) : base(dictionary)
    {

    }
}
