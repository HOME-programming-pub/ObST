using ObST.Tester.Core.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ObST.Tester.Test.Core.Models;

[TestClass]
public class TestParameterGeneratorOptionEqualityTest
{

    [TestMethod]
    public void EmptyInstances_SameHashCode()
    {
        var o1 = new TestParameterGeneratorOption();
        var o2 = new TestParameterGeneratorOption();

        o1.GetHashCode().Should().Be(o2.GetHashCode());
    }

    [TestMethod]
    public void EmptyInstances_AreEqual()
    {
        var o1 = new TestParameterGeneratorOption();
        var o2 = new TestParameterGeneratorOption();

        o1.Equals(o2).Should().BeTrue();
    }

    [TestMethod]
    public void InstancesWithEntryOutOfOrder_SameHashCode()
    {
        var o1 = new TestParameterGeneratorOption
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "something") }
        };

        var o2 = new TestParameterGeneratorOption 
        {
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "something") },
            {"a", (GeneratorMode.Random, null) }
        };

        o1.GetHashCode().Should().Be(o2.GetHashCode());
    }

    [TestMethod]
    public void InstancesWithEntryOutOfOrder_AreEqual()
    {
        var o1 = new TestParameterGeneratorOption
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "something") }
        };

        var o2 = new TestParameterGeneratorOption 
        {
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "something") },
            {"a", (GeneratorMode.Random, null) }
        };

        o1.Equals(o2).Should().BeTrue();
    }

    [TestMethod]
    public void InstancesWithDifferentEntries_DifferentHashCode()
    {
        var o1 = new TestParameterGeneratorOption
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "other") }
        };

        var o2 = new TestParameterGeneratorOption 
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "something") }
        };

        o1.GetHashCode().Should().NotBe(o2.GetHashCode());
    }

    [TestMethod]
    public void InstancesWithDifferentKeys_DifferentHashCode()
    {
        var o1 = new TestParameterGeneratorOption
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "something") }
        };

        var o2 = new TestParameterGeneratorOption 
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"d", (GeneratorMode.UseConstant, "something") },
        };

        o1.GetHashCode().Should().NotBe(o2.GetHashCode());
    }

    [TestMethod]
    public void InstancesWithDifferentEntryies_AreNotEqual()
    {
        var o1 = new TestParameterGeneratorOption
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "other") }
        };

        var o2 = new TestParameterGeneratorOption 
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "something") }
        };

        o1.Equals(o2).Should().BeFalse();
    }

    [TestMethod]
    public void InstancesWithDifferentKey_AreNotEqual()
    {
        var o1 = new TestParameterGeneratorOption
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "something") }
        };

        var o2 = new TestParameterGeneratorOption 
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"d", (GeneratorMode.UseConstant, "something") }
        };

        o1.Equals(o2).Should().BeFalse();
    }

    [TestMethod]
    public void InstancesWithCastedValue_SameHashCode()
    {
        var o1 = new TestParameterGeneratorOption
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, new string("-123")) }
        };

        var o2 = new TestParameterGeneratorOption 
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "-123") }
        };

        o1.GetHashCode().Should().Be(o2.GetHashCode());
    }

    [TestMethod]
    public void InstancesWithCastedValue_AreEqual()
    {
        var o1 = new TestParameterGeneratorOption
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, new string("-123")) }
        };

        var o2 = new TestParameterGeneratorOption 
        {
            {"a", (GeneratorMode.Random, null) },
            {"b", (GeneratorMode.RequireUnknown, null) },
            {"c", (GeneratorMode.UseConstant, "-123") }
        };

        o1.Equals(o2).Should().BeTrue();
    }
}
