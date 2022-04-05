using ObST.Core.Models;

namespace ObST.Tester.Core.Interfaces;

interface ITestConfigurationProvider
{
    TestConfiguration? TestConfiguration { get; set; }
}
