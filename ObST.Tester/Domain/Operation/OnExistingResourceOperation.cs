using ObST.Core.Models;
using ObST.Tester.Core.Interfaces;
using ObST.Tester.Core.Models;
using ObST.Tester.Domain.Util;
using FsCheck;
using Microsoft.OpenApi.Models;
using System.Net;

namespace ObST.Tester.Domain.Operation;

class OnExistingResourceOperation : TestOperation
{
    public OnExistingResourceOperation(SutOperation operation, TestConfiguration config, IIdentityConnector identityConnector) : base(operation, config, identityConnector)
    {
    }

    protected override FastFailProperty CheckResult(RunActualResult res, TestModel model)
    {
        if (res.Response.IsSuccessStatusCode)
        {
            if (res.Response.StatusCode == HttpStatusCode.Created)
            {
                var location = res.Response.Headers.Location;

                if (location != null)
                    model.ExtractIdsFromUri(location, _config);
            }

            //TODO respect 204 Accepted
            if (Operation.OperationType == OperationType.Delete && Operation.Path.EndsWith("{?}"))
                model.Delete(GetPathValues());
            else if (res.BodyMatchesSchema && res.BodyObject != null)
                model.SearchForIds(res.BodyObject, res.DocumentedSchema!, GetPathValues());
        }

        if (Operation.OperationType == OperationType.Get)
            return res.Response.IsSuccessStatusCode
                .FastFailWhen(_config.Setup?.Properties?.NoBadRequestWhenValidDataIsProvided == true || res.Response.StatusCode != HttpStatusCode.BadRequest && res.Response.StatusCode != HttpStatusCode.UnprocessableEntity)
                .Label("Expected success status code on existing resource")
                .Label("Actual StatusCode: " + (int)res.Response.StatusCode);
        else
            return true.ToFastFailProperty();
    }

    public override string ToString()
    {
        return "[E] " + base.ToString();
    }

    public override IList<TestParameterGeneratorOption> GetGeneratorOptions(TestModel model)
    {
        var baseOpt = new TestParameterGeneratorOption();

        var needs = Operation.GetNeeds();

        foreach (var s in Operation.UniqueParameters.Except(needs))
            baseOpt.Add(s.Mapping, s.ParameterType.HasFlag(ParameterType.SelfReference) ? GeneratorMode.RequireUnknown : GeneratorMode.Random);

        if (!needs.Any())
            //empty option
            return new List<TestParameterGeneratorOption> { baseOpt };

        return model.GetMatchingIdSubset(needs)
            .Where(ids => ids.All(id => !id.Value.deleted))
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
