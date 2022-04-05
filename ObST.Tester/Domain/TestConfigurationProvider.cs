using ObST.Core.Models;
using ObST.Tester.Core.Interfaces;

namespace ObST.Tester.Domain;

class TestConfigurationProvider : ITestConfigurationProvider
{
    public TestConfiguration? TestConfiguration { get; set; }
}
