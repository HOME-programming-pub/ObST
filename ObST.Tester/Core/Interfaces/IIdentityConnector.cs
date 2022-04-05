using ObST.Tester.Core.Models;

namespace ObST.Tester.Core.Interfaces;

interface IIdentityConnector
{
    Task<string> GetIdentityInformationAsync(SutIdentity identity);
}
