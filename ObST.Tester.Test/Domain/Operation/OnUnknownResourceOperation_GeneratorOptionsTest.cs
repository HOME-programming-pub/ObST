using ObST.Tester.Core.Models;
using ObST.Tester.Domain;
using ObST.Tester.Domain.Operation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ObST.Tester.Test.Domain.Operation;

[TestClass]
public class OnUnknownResourceOperation_GeneratorOptionsTest
{

    [TestMethod]
    public void NoPaths()
    {
        var sutOp = CreateOperation(new List<UniqueParameter>
        {
            new UniqueParameter("main:id", null!)
            {
                ParameterType = ParameterType.Reference,
                IsInResourceIdentifier = true,
            }
        });

        var op = new OnUnknownResourceOperation(sutOp, null!, null!);

        var model = new TestModel();

        var actual = op.GetGeneratorOptions(model);

        actual.Should().ContainSingle()
            .Which.Should().HaveCount(1)
            .And.ContainKey("main:id")
            .WhoseValue.mode.Should().Be(GeneratorMode.RequireUnknown);
    }

    //[TestMethod]
    //public void SinglePath()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) }
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    model.Add(new[] { ("main:id", "123") });

    //    var actual = op.GetGeneratorOptions(model);

    //    actual.IdOptions.Should().ContainSingle()
    //        .Which.Should().HaveCount(1)
    //        .And.ContainKey("main:id")
    //        .WhichValue.mode.Should().Be(IdMode.RequiereUnknown);
    //}

    //[TestMethod]
    //public void NoPaths_NestedObject()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:child:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) },
    //                {"main:child:id", (true, true, null) },
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    var actual = op.GetGeneratorOptions(model);

    //    var option = actual.IdOptions.Should().ContainSingle().Which;

    //    option.Should().HaveCount(2);

    //    option.Should().ContainKey("main:id")
    //        .WhichValue.mode.Should().Be(IdMode.RequiereUnknown);

    //    option.Should().ContainKey("main:child:id")
    //      .WhichValue.mode.Should().Be(IdMode.RequiereUnknown);
    //}

    //[TestMethod]
    //public void SinglePath_NestedObject()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:child:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) },
    //                {"main:child:id", (true, true, null) },
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    model.Add(new[] { ("main:id", "123"), ("main:child:id", "456") });

    //    var actual = op.GetGeneratorOptions(model);

    //    actual.IdOptions.Should().HaveCount(3)
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "123") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "456") },
    //        });
    //}

    //[TestMethod]
    //public void TwoPaths_NestedObject()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:child:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) },
    //                {"main:child:id", (true, true, null) },
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    model.Add(new[] { ("main:id", "123"), ("main:child:id", "456") });
    //    model.Add(new[] { ("main:id", "321"), ("main:child:id", "654") });

    //    var actual = op.GetGeneratorOptions(model);

    //    actual.IdOptions.Should().HaveCount(7)
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "123") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "456") },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "321") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "654") },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "123") },
    //            {"main:child:id", (IdMode.UseConstant, "654") },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "321") },
    //            {"main:child:id", (IdMode.UseConstant, "456") },
    //        });
    //}

    //[TestMethod]
    //public void TwoPaths_NestedObject_SameParent()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:child:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) },
    //                {"main:child:id", (true, true, null) },
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    model.Add(new[] { ("main:id", "123"), ("main:child:id", "4") });
    //    model.Add(new[] { ("main:id", "123"), ("main:child:id", "5") });

    //    var actual = op.GetGeneratorOptions(model);

    //    actual.IdOptions.Should().HaveCount(4)
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "123") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "4") },
    //        })
    //         .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "5") },
    //        });
    //}

    //[TestMethod]
    //public void TwoPaths_NestedObject_SameChildId()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:child:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) },
    //                {"main:child:id", (true, true, null) },
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    model.Add(new[] { ("main:id", "123"), ("main:child:id", "a") });
    //    model.Add(new[] { ("main:id", "456"), ("main:child:id", "a") });

    //    var actual = op.GetGeneratorOptions(model);

    //    actual.IdOptions.Should().HaveCount(4)
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "123") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //            .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "456") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "a") },
    //        });
    //}        //[TestMethod]
    //public void NoPaths()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) }
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    var actual = op.GetGeneratorOptions(model);

    //    actual.IdOptions.Should().ContainSingle()
    //        .Which.Should().HaveCount(1)
    //        .And.ContainKey("main:id")
    //        .WhichValue.mode.Should().Be(IdMode.RequiereUnknown);
    //}

    //[TestMethod]
    //public void SinglePath()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) }
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    model.Add(new[] { ("main:id", "123") });

    //    var actual = op.GetGeneratorOptions(model);

    //    actual.IdOptions.Should().ContainSingle()
    //        .Which.Should().HaveCount(1)
    //        .And.ContainKey("main:id")
    //        .WhichValue.mode.Should().Be(IdMode.RequiereUnknown);
    //}

    //[TestMethod]
    //public void NoPaths_NestedObject()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:child:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) },
    //                {"main:child:id", (true, true, null) },
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    var actual = op.GetGeneratorOptions(model);

    //    var option = actual.IdOptions.Should().ContainSingle().Which;

    //    option.Should().HaveCount(2);

    //    option.Should().ContainKey("main:id")
    //        .WhichValue.mode.Should().Be(IdMode.RequiereUnknown);

    //    option.Should().ContainKey("main:child:id")
    //      .WhichValue.mode.Should().Be(IdMode.RequiereUnknown);
    //}

    //[TestMethod]
    //public void SinglePath_NestedObject()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:child:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) },
    //                {"main:child:id", (true, true, null) },
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    model.Add(new[] { ("main:id", "123"), ("main:child:id", "456") });

    //    var actual = op.GetGeneratorOptions(model);

    //    actual.IdOptions.Should().HaveCount(3)
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "123") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "456") },
    //        });
    //}

    //[TestMethod]
    //public void TwoPaths_NestedObject()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:child:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) },
    //                {"main:child:id", (true, true, null) },
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    model.Add(new[] { ("main:id", "123"), ("main:child:id", "456") });
    //    model.Add(new[] { ("main:id", "321"), ("main:child:id", "654") });

    //    var actual = op.GetGeneratorOptions(model);

    //    actual.IdOptions.Should().HaveCount(7)
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "123") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "456") },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "321") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "654") },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "123") },
    //            {"main:child:id", (IdMode.UseConstant, "654") },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "321") },
    //            {"main:child:id", (IdMode.UseConstant, "456") },
    //        });
    //}

    //[TestMethod]
    //public void TwoPaths_NestedObject_SameParent()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:child:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) },
    //                {"main:child:id", (true, true, null) },
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    model.Add(new[] { ("main:id", "123"), ("main:child:id", "4") });
    //    model.Add(new[] { ("main:id", "123"), ("main:child:id", "5") });

    //    var actual = op.GetGeneratorOptions(model);

    //    actual.IdOptions.Should().HaveCount(4)
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "123") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "4") },
    //        })
    //         .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "5") },
    //        });
    //}

    //[TestMethod]
    //public void TwoPaths_NestedObject_SameChildId()
    //{
    //    var sutOp = new SutOperation
    //    {
    //        Parameters = new SutParameters
    //        {
    //            PrimaryResourceId = "main:child:id",
    //            UniqueParameters = new Dictionary<string, (bool isId, bool isInUrl, Microsoft.OpenApi.Models.OpenApiSchema schema)>
    //            {
    //                {"main:id", (true, true, null) },
    //                {"main:child:id", (true, true, null) },
    //            }
    //        }
    //    };

    //    var op = new OnUnknownResourceOperation(sutOp, null, null);

    //    var model = new TestModel();

    //    model.Add(new[] { ("main:id", "123"), ("main:child:id", "a") });
    //    model.Add(new[] { ("main:id", "456"), ("main:child:id", "a") });

    //    var actual = op.GetGeneratorOptions(model);

    //    actual.IdOptions.Should().HaveCount(4)
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "123") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //            .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.UseConstant, "456") },
    //            {"main:child:id", (IdMode.RequiereUnknown, null) },
    //        })
    //        .And.ContainEquivalentOf(
    //        new Dictionary<string, (IdMode, string)>
    //        {
    //            {"main:id", (IdMode.RequiereUnknown, null) },
    //            {"main:child:id", (IdMode.UseConstant, "a") },
    //        });
    //}

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