using ObST.Core.Models;
using ObST.Tester.Core.Interfaces;
using ObST.Tester.Core.Models;
using ObST.Tester.Domain.Util;
using FsCheck;
using System.Net;

namespace ObST.Tester.Domain.Operation;

class OnDeletedResourceOperation : TestOperation
{
    public OnDeletedResourceOperation(SutOperation operation, TestConfiguration config, IIdentityConnector identityConnector) : base(operation, config, identityConnector)
    {
    }

    protected override FastFailProperty CheckResult(RunActualResult res, TestModel model)
    {
        return (res.Response.StatusCode == HttpStatusCode.NotFound || res.Response.StatusCode == HttpStatusCode.Gone)
            .FastFailWhen(_config.Setup?.Properties?.NoBadRequestWhenValidDataIsProvided == true || res.Response.StatusCode != HttpStatusCode.BadRequest && res.Response.StatusCode != HttpStatusCode.UnprocessableEntity)
            .Label("Expected 404 or 410 on deleted resource")
            .Label("Actual StatusCode: " + (int)res.Response.StatusCode);
    }

    public override bool Pre(TestModel model)
    {
        return base.Pre(model);
    }

    public override string ToString()
    {
        return "[D] " + base.ToString();
    }

    public override IList<TestParameterGeneratorOption> GetGeneratorOptions(TestModel model)
    {
        var baseOpt = new TestParameterGeneratorOption();

        var needs = Operation.GetNeeds();

        foreach (var s in Operation.UniqueParameters.Except(needs))
            baseOpt.Add(s.Mapping, s.ParameterType.HasFlag(ParameterType.SelfReference) ? GeneratorMode.RequireUnknown : GeneratorMode.Random);

        if (!needs.Any())
        {
            //no option
            return new List<TestParameterGeneratorOption> { };
        }

        return model.GetMatchingIdSubset(needs)
            .Where(ids => ids.Any(id => id.Value.deleted && needs.Where(n => n.IsInResourceIdentifier).Any(n => n.Mapping == id.Key)))
            .Select(ids =>
            {
                var option = new TestParameterGeneratorOption(baseOpt);

                foreach (var p in ids)
                    option.AddConstant(p.Key, p.Value.value!);

                return option;
            })
            .ToList();
    }
}
