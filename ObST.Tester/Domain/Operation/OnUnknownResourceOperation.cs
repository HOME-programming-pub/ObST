using ObST.Core.Models;
using ObST.Tester.Core.Interfaces;
using ObST.Tester.Core.Models;
using ObST.Tester.Domain.Util;
using FsCheck;
using System.Net;

namespace ObST.Tester.Domain.Operation;

class OnUnknownResourceOperation : TestOperation
{
    public OnUnknownResourceOperation(SutOperation operation, TestConfiguration config, IIdentityConnector identityConnector) : base(operation, config, identityConnector)
    {
    }

    protected override FastFailProperty CheckResult(RunActualResult res, TestModel model)
    {
        return (res.Response.StatusCode == HttpStatusCode.NotFound)
            .FastFailWhen(_config.Setup?.Properties?.NoBadRequestWhenValidDataIsProvided == true || res.Response.StatusCode != HttpStatusCode.BadRequest && res.Response.StatusCode != HttpStatusCode.UnprocessableEntity)
            .Label("Expected 404 on unknown resource")
            .Label("Actual StatusCode: " + (int)res.Response.StatusCode);
    }

    public override bool Pre(TestModel model)
    {
        return base.Pre(model);
    }

    public override string ToString()
    {
        return "[U] " + base.ToString();
    }

    public override IList<TestParameterGeneratorOption> GetGeneratorOptions(TestModel model)
    {
        var option = new TestParameterGeneratorOption();

        var needs = Operation.GetNeeds();

        if (!needs.Any())
        {
            //no options option
            return new List<TestParameterGeneratorOption> { };
        }

        foreach (var p in Operation.UniqueParameters)
            option.Add(p.Mapping, needs.Contains(p) ? GeneratorMode.RequireUnknown : GeneratorMode.Random);

        return new List<TestParameterGeneratorOption> { option };

        //if (needs.Count > 1)
        //{
        //    var paths = model
        //        .GetPossiblePaths()
        //        .Where(p => p.Count() > 1 && p.All(v => !v.deleted))
        //        .ToArray();

        //    foreach (var p in paths)
        //    {
        //        var path = p.ToArray();

        //        if (path.Length == needs.Count)
        //        {
        //            var isMatch = true;

        //            for (int i = 0; i < path.Length; i++)
        //                if (path[i].key != needs[i].mapping)
        //                    isMatch = false;

        //            if (!isMatch)
        //                break;

        //            //ignore trivial (all unknown when i = 0 | all known when i = path.Length * path.Length -1)
        //            for (int i = 1; i < (path.Length * path.Length - 1); i++)
        //            {
        //                var option = new Dictionary<string, (IdMode, string)>();

        //                for (int k = 0; k < path.Length; k++)
        //                {
        //                    if ((i & (k + 1)) > 0)
        //                        option.Add(path[k].key, (IdMode.UseConstant, path[k].value));
        //                    else
        //                        option.Add(path[k].key, (IdMode.RequiereUnknown, null));
        //                }

        //                //ensure list is distinct
        //                if (!idOptions.Any(o => o.OrderBy(o => o.Key).SequenceEqual(option.OrderBy(o => o.Key))))
        //                    idOptions.Add(option);
        //            }
        //        }
        //    }

        //    var groups = paths.GroupBy(p => string.Join(',', p.Select(v => v.key)));

        //    foreach (var group in groups.Where(g => g.Count() > 1))
        //    {
        //        var path = group.First().ToArray();

        //        if (path.Length != needs.Count)
        //            continue;

        //        var isMatch = true;

        //        for (int i = 0; i < path.Length; i++)
        //            if (path[i].key != needs[i].mapping)
        //                isMatch = false;

        //        if (!isMatch)
        //            continue;

        //        var g = group.ToArray();

        //        for (int n = 0; n < group.Count(); n++)
        //            for (int m = 0; m < n; m++)
        //            {
        //                var path0 = g[n].ToArray();
        //                var path1 = g[m].ToArray();

        //                for (int i = 1; i < (path.Length * path.Length - 1); i++)
        //                {
        //                    var option = new Dictionary<string, (IdMode, string)>();
        //                    var matchingPaths = paths.Where(p => p.Count() == path.Length);

        //                    for (int k = 0; k < path.Length; k++)
        //                    {
        //                        var value = (i & (k + 1)) > 0 ? path0[k].value : path1[k].value;

        //                        matchingPaths = matchingPaths.Where(p =>
        //                        {
        //                            var current = p.Skip(k).First();
        //                            return current.key == path[k].key && current.value == value;
        //                        }).ToList();

        //                        option.Add(path[k].key, (IdMode.UseConstant, value));
        //                    }

        //                    //validate that no real path exists for this combination and ensure list is distinct
        //                    if (!matchingPaths.Any() && !idOptions.Any(o => o.OrderBy(o => o.Key).SequenceEqual(option.OrderBy(o => o.Key))))
        //                        idOptions.Add(option);
        //                }
        //            }
        //    }
        //}
    }
}
