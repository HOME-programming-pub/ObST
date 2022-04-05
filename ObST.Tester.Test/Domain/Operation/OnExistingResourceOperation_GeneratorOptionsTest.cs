using ObST.Tester.Core.Models;
using ObST.Tester.Domain;
using ObST.Tester.Domain.Operation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ObST.Tester.Test.Domain.Operation;

[TestClass]
public class OnExistingResourceOperation_GeneratorOptionsTest
{

    [TestMethod]
    public void NoPaths()
    {
        var sutOp = CreateOperation(new List<UniqueParameter>
        {
            new UniqueParameter("main:id", null!)
            {
                ParameterType = ParameterType.Reference,
                IsRequired = true
            }
        });

        var op = new OnExistingResourceOperation(sutOp, null!, null!);

        var model = new TestModel();

        var actual = op.GetGeneratorOptions(model);

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public void SinglePath()
    {
        var sutOp = CreateOperation(new List<UniqueParameter>
        {
            new UniqueParameter("main:id", null!)
            {
                ParameterType = ParameterType.Reference,
                IsRequired = true
            }
        });

        var op = new OnExistingResourceOperation(sutOp, null!, null!);

        var model = new TestModel();
        model.Add(new[] { ("main:id", "123") });

        var actual = op.GetGeneratorOptions(model);

        actual.Should().ContainSingle()
            .Which.Should().HaveCount(1)
            .And.ContainKey("main:id")
            .WhoseValue.Should().Be((GeneratorMode.UseConstant, "123"));
    }

    [TestMethod]
    public void SingleDeletedPath()
    {
        var sutOp = CreateOperation(new List<UniqueParameter>
        {
            new UniqueParameter("main:id", null!)
            {
                ParameterType = ParameterType.Reference,
                IsRequired = true

            }
        });

        var op = new OnExistingResourceOperation(sutOp, null!, null!);

        var model = new TestModel();
        model.Add(new[] { ("main:id", "123") });
        model.Delete(new[] { ("main:id", "123") });

        var actual = op.GetGeneratorOptions(model);

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public void SinglePathWithParentConstraint()
    {
        var c1 = new UniqueParameter("main:id", null!)
        {
            ParameterType = ParameterType.Reference,
            IsRequired = true
        };

        var c2 = new UniqueParameter("other:id", null!)
        {
            ParameterType = ParameterType.Reference,
            IsRequired = true
        };

        c1.LinkChild(c2);

        var sutOp = CreateOperation(new List<UniqueParameter>
        {
            c1,
            c2
        });

        var op = new OnExistingResourceOperation(sutOp, null!, null!);

        var model = new TestModel();
        model.Add(new[] { ("main:id", "123"), ("other:id", "456") });

        var actual = op.GetGeneratorOptions(model);

        actual.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new TestParameterGeneratorOption
            {
                { "main:id", (GeneratorMode.UseConstant, "123") },
                { "other:id", (GeneratorMode.UseConstant, "456") }
            });
    }

    [TestMethod]
    public void MultiplePathsWithParentConstraint()
    {
        var c1 = new UniqueParameter("main:id", null!)
        {
            ParameterType = ParameterType.Reference,
            IsRequired = true
        };

        var c2 = new UniqueParameter("other:id", null!)
        {
            ParameterType = ParameterType.Reference,
            IsRequired = true
        };

        c1.LinkChild(c2);

        var sutOp = CreateOperation(new List<UniqueParameter>
        {
            c1,
            c2
        });

        var op = new OnExistingResourceOperation(sutOp, null!, null!);

        var model = new TestModel();
        model.Add(new[] { ("main:id", "1"), ("other:id", "4") });
        model.Add(new[] { ("main:id", "2") });
        model.Add(new[] { ("main:id", "3"), ("other:id", "6") });

        var actual = op.GetGeneratorOptions(model);

        actual.Should().HaveCount(2)
            .And.ContainEquivalentOf(new TestParameterGeneratorOption
            {
                { "main:id", (GeneratorMode.UseConstant, "1") },
                { "other:id", (GeneratorMode.UseConstant, "4") }
            })
             .And.ContainEquivalentOf(new TestParameterGeneratorOption
            {
                { "main:id", (GeneratorMode.UseConstant, "3") },
                { "other:id", (GeneratorMode.UseConstant, "6") }
            });
    }

    [TestMethod]
    public void SinglePathWithParentAndUnspecifiedConstraint()
    {
        var c1 = new UniqueParameter("main:id", null!)
        {
            ParameterType = ParameterType.Reference,
            IsRequired = true
        };

        var c2 = new UniqueParameter("other:id", null!)
        {
            ParameterType = ParameterType.Reference,
            IsRequired = true
        };

        var c3 = new UniqueParameter("NonTree:id", null!)
        {
            ParameterType = ParameterType.Reference,
            IsRequired = true
        };

        c1.LinkChild(c2);

        var sutOp = CreateOperation(new List<UniqueParameter>
        {
            c1,
            c2,
            c3
        });

        var op = new OnExistingResourceOperation(sutOp, null!, null!);

        var model = new TestModel();
        model.Add(new[] { ("main:id", "123"), ("other:id", "456") });
        model.Add(new[] { ("NonTree:id", "333") });

        var actual = op.GetGeneratorOptions(model);

        actual.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new TestParameterGeneratorOption
            {
                { "main:id", (GeneratorMode.UseConstant, "123") },
                { "other:id", (GeneratorMode.UseConstant, "456") },
                { "NonTree:id", (GeneratorMode.UseConstant, "333") }
            });
    }

    private static SutOperation CreateOperation(List<UniqueParameter> uniqueParameters)
    {
        return new SutOperation(
            "OperationId",
            Microsoft.OpenApi.Models.OperationType.Get,
            "Path",
            null!,
            null!,
            uniqueParameters,
            null!,
            null!,
            false,
            null!);
    }
}
