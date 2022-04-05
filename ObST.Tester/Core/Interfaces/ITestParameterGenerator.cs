using ObST.Tester.Core.Models;
using ObST.Tester.Domain;
using FsCheck;

namespace ObST.Tester.Core.Interfaces;

interface ITestParameterGenerator
{
    Gen<SutOperationValues> Build(SutOperation operation, TestModel model, IList<TestParameterGeneratorOption> options, IList<SutIdentity> identities);
}
