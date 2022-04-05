using ObST.Core.Models;

namespace ObST.Core.Interfaces;

interface ITestConfigurationReader
{
    Task<TestConfiguration?> ReadAsync(string path);
}
