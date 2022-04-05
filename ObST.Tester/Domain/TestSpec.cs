using ObST.Tester.Core.Interfaces;
using ObST.Tester.Core.Models;
using ObST.Tester.Domain.Operation;
using ObST.Tester.Domain.Util;
using FsCheck;
using FsCheck.Experimental;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Collections;
using Microsoft.OpenApi.Models;

namespace ObST.Tester.Domain;

class TestSpec : Machine<ISutConnector, TestModel>
{
    private readonly ILogger _logger;

    private readonly IList<(int weight, Gen<TestOperation> gen, TestOperation op)> _onExistingOperations;
    private readonly IList<(int weight, Gen<TestOperation> gen, TestOperation op)> _onDeletedOperations;
    private readonly IList<(int weight, Gen<TestOperation> gen, TestOperation op)> _onUnknownOperations;
    private readonly IList<SutIdentity> _identities;
    private readonly ITestParameterGenerator _parameterGenerator;
    private readonly ISutConnector _sutConnector;
    private readonly ITestConfigurationProvider _testConfigurationProvider;


    public TestSpec(ITestConfigurationProvider testConfigurationProvider, ITestParameterGenerator parameterGenerator, ISutConnector sutConnector, IIdentityConnector identityConnector, ILogger<TestSpec> logger)
    {
        _logger = logger;

        _testConfigurationProvider = testConfigurationProvider;
        _parameterGenerator = parameterGenerator;
        _sutConnector = sutConnector;

        _onExistingOperations = new List<(int weight, Gen<TestOperation> gen, TestOperation op)>();
        _onDeletedOperations = new List<(int weight, Gen<TestOperation> gen, TestOperation op)>();
        _onUnknownOperations = new List<(int weight, Gen<TestOperation> gen, TestOperation op)>();

        _logger.LogDebug("Mapping operations to SutOperations:");

        var config = testConfigurationProvider.TestConfiguration!;

        _identities = config.ToSutIndentities();

        foreach (var op in config.ToSutOperations(_identities))
        {
            if (!op.ValidIdentities.Any())
                _logger.LogWarning("{operationId}: No valid identity specified, method will only be tested with invalid identiy", op.OperationId);

            var needs = op.GetNeeds();

            _logger.LogDebug("{operationId} Params: {parameters}", op.OperationId, string.Join(", ", op.UniqueParameters.Select(n => n.ToString())));


            var weight = op.DoesCreate ? 10 : op.OperationType == OperationType.Get ? 5 : op.OperationType == OperationType.Delete ? 2 : 3;

            _onExistingOperations.AddOperation(weight, () => new OnExistingResourceOperation(op, config, identityConnector));

            if (needs.Any(n => n.IsInResourceIdentifier && n.IsRequired))
            {
                _onUnknownOperations.AddOperation(1, () => new OnUnknownResourceOperation(op, config, identityConnector));
                _onDeletedOperations.AddOperation(1, () => new OnDeletedResourceOperation(op, config, identityConnector));
            }
        }
    }

    public override Arbitrary<Setup<ISutConnector, TestModel>> Setup => Gen.Fresh(() => StateMachine.Setup(
        () =>
        {
            _sutConnector.ResetAsync().GetAwaiter().GetResult();
            return _sutConnector;
        },
        () => new TestModel()
        )).ToArbitrary();

    public override Gen<Operation<ISutConnector, TestModel>> Next(TestModel model)
    {
        var onExistingOperations = _onExistingOperations.Where(o => o.op.Pre(model)).Select(o => Tuple.Create(o.weight, o.gen)).ToList();
        var onUnknownOperations = _onUnknownOperations.Where(o => o.op.Pre(model)).Select(o => Tuple.Create(o.weight, o.gen)).ToList();
        var onDeletedOperations = _onDeletedOperations.Where(o => o.op.Pre(model)).Select(o => Tuple.Create(o.weight, o.gen)).ToList();

        var opTypes = new List<Tuple<int, Gen<TestOperation>>>();

        if (onExistingOperations.Any())
            opTypes.Add(Tuple.Create(90, Gen.Frequency(onExistingOperations)));

        if (onUnknownOperations.Any())
            opTypes.Add(Tuple.Create(2, Gen.Frequency(onUnknownOperations)));

        if (onDeletedOperations.Any())
            opTypes.Add(Tuple.Create(8, Gen.Frequency(onDeletedOperations)));

        return Gen
            .Frequency(opTypes)
            .SelectMany(op => _parameterGenerator.Build(op.Operation, model, op.GetGeneratorOptions(model), _identities)
                .Select(values =>
                {
                    op.SetParameters(values);
                    return (Operation<ISutConnector, TestModel>)op;
                })
            );
    }

    public Property ToPropertyWithoutModelDryRun()
    {
        var arb = Arb.From(Generate(), (run) => Shrink(run));

        return Prop.ForAll(arb, (run) => run.Property);
    }

    private Gen<PreCalculatedRun> Generate()
    {
        return Gen.Sized(size => Setup.Generator.SelectMany(setup =>
        {
            var initialModel = setup.Model();
            var actual = setup.Actual();
            var usedSize = MaxNumberOfCommands < 0 ? size : MaxNumberOfCommands;

            var gen = Gen.Fresh(() => new List<Tuple<Operation<ISutConnector, TestModel>, TestModel>>())
                .SelectMany(list => GenCommands(initialModel, actual, list, usedSize, Prop.OfTestable(true)));

            return gen.Select(res =>
            {
                TearDown.Actual(actual);

                var l = res.operations.Count();

                var property = res.property
                    .Trivial(l == 0)
                    .Classify(l > 1 && l <= 6, "short sequences (between 1-6 commands)")
                    .Classify(l > 6, "long sequences (>6 commands)")
                    .Classify(-1 == usedSize && l < usedSize, "aborted sequences")
                    .Classify(-1 == usedSize && l > usedSize, "longer than used size sequences (should not occur)")
                    .Classify(-1 == usedSize, "artificial sequences");

                return new PreCalculatedRun
                (
                    new MachineRun<ISutConnector, TestModel>(Tuple.Create(initialModel, setup), ListModule.OfSeq(res.operations), TearDown, usedSize),
                    property
                );
            });
        }));
    }

    private Gen<(List<Tuple<Operation<ISutConnector, TestModel>, TestModel>> operations, Property property)> GenCommands(TestModel model, ISutConnector actual, List<Tuple<Operation<ISutConnector, TestModel>, TestModel>> list, int size, Property property)
    {
        if (size <= 0)
            return Gen.Constant((list, property));
        else
            return
                Next(model)
                .SelectMany(op =>
                {
                    if (op is StopOperation<ISutConnector, TestModel>)
                        return Gen.Constant((list, property));
                    else
                    {
                        model = op.Run(model);

                        ((IOperation)op).ClearDependencies(); //side-effect :(

                            //Check executes the op on actual 
                            var prop = ((TestOperation)op).CheckWithFastFail(actual, model);

                        property = property.And(prop.Property);
                        list.Add(Tuple.Create(op, model));

                        if (prop.IsSuccess)
                            return GenCommands(model, actual, list, size - 1, property);
                        else
                            return Gen.Constant((list, property));
                    }
                });
    }


    private IEnumerable<PreCalculatedRun> Shrink(PreCalculatedRun run)
    {
        if (_testConfigurationProvider.TestConfiguration?.Setup?.QuickCheck?.DoNotShrink == true)
            yield break;

        var setup = run.Run.Setup;
        var ops = ListModule.OfSeq(run.Run.Operations.Select(o => o.Item1));

        //try to shrink the list of operations
        foreach (var op in ShrinkOperations(ops))
        {
            var newRun = ChooseModels(setup, op, run.Run.UsedSize);

            if (newRun != null)
                yield return newRun;
        }

        //try to srhink the initial setup state
        foreach (var s in Arb.toShrink(Setup).Invoke(setup.Item2))
        {
            var newRun = ChooseModels(new Tuple<TestModel, Setup<ISutConnector, TestModel>>(s.Model(), s), ops, run.Run.UsedSize);

            if (newRun != null)
                yield return newRun;
        }
    }

    private PreCalculatedRun? ChooseModels(Tuple<TestModel, Setup<ISutConnector, TestModel>> setup, FSharpList<Operation<ISutConnector, TestModel>> ops, int usedSize)
    {
        ISutConnector? actual = null;
        var model = setup.Item1;

        var provided = new HashSet<IOperationResult>();
        var newOperations = new List<Tuple<Operation<ISutConnector, TestModel>, TestModel>>();

        var property = Prop.OfTestable(true);

        foreach (var op in ops)
        {
            var hasNeeds = provided.IsSupersetOf(((IOperation)op).Needs);

            if (hasNeeds && op.Pre(model))
            {
                //only init when necessary
                if (actual == null)
                    actual = setup.Item2.Actual();

                //add provided
                provided.UnionWith(((IOperation)op).Provides);
                model = op.Run(model);

                ((IOperation)op).ClearDependencies(); //side-effect :(

                //Check executes the op on actual 
                property = property.And(op.Check(actual, model));

                newOperations.Add(new Tuple<Operation<ISutConnector, TestModel>, TestModel>(op, model));
            }
        }

        if (actual != null)
        {
            TearDown.Actual(actual);

            var l = newOperations.Count();

            property = property
                .Trivial(l == 0)
                .Classify(l > 1 && l <= 6, "short sequences (between 1-6 commands)")
                .Classify(l > 6, "long sequences (>6 commands)")
                .Classify(-1 == usedSize && l < usedSize, "aborted sequences")
                .Classify(-1 == usedSize && l > usedSize, "longer than used size sequences (should not occur)")
                .Classify(-1 == usedSize, "artificial sequences");

            return new PreCalculatedRun
            (
                new MachineRun<ISutConnector, TestModel>(setup, ListModule.OfSeq(newOperations), TearDown, usedSize),
                property
            );
        }
        else
            return null;
    }


    private record PreCalculatedRun
    {
        public MachineRun<ISutConnector, TestModel> Run { get; }
        public Property Property { get; }

        public PreCalculatedRun(MachineRun<ISutConnector, TestModel> run, Property property)
        {
            Run = run;
            Property = property;
        }

        public override string ToString()
        {
            return Run.ToString();
        }
    }

}
