using ObST.Core.Models;

namespace ObST.Core.Interfaces;

interface ITestConfigurationWriter
{
    Task WriteAsync(string path, TestConfiguration config);
}
