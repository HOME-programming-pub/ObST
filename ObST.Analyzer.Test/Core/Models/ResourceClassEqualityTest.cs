using ObST.Analyzer.Core.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ObST.Analyzer.Test.Core.Models;

[TestClass]
public class ResourceClassEqualityTest
{
    [TestMethod]
    public void SimpleMatch()
    {
        var r1 = new ResourceClass
        {
            Name = "Name"
        };

        var r2 = new ResourceClass
        {
            Name = "Name"
        };

        r1.Should().Be(r2);
    }

    [TestMethod]
    public void SimpleUnmatch()
    {
        var r1 = new ResourceClass
        {
            Name = "Name"
        };

        var r2 = new ResourceClass
        {
            Name = "Other"
        };

        r1.Should().NotBe(r2);
    }

    [TestMethod]
    public void SubordinateMatch()
    {
        var r1 = new ResourceClass
        {
            Subordinate = new ResourceClass
            {
                Name = "Name"
            }
        };

        var r2 = new ResourceClass
        {
            Subordinate = new ResourceClass
            {
                Name = "Name"
            }
        };

        r1.Should().Be(r2);
    }

    [TestMethod]
    public void SubordinateUnmatch()
    {
        var r1 = new ResourceClass
        {
            Subordinate = new ResourceClass
            {
                Name = "Name"
            }
        };

        var r2 = new ResourceClass
        {
            Subordinate = new ResourceClass
            {
                Name = "Other"
            }
        };

        r1.Should().NotBe(r2);
    }
    [TestMethod]
    public void SubordinateWithSimpleUnmatch()
    {
        var r1 = new ResourceClass
        {
            Subordinate = new ResourceClass
            {
                Name = "Name"
            }
        };

        var r2 = new ResourceClass
        {
            Name = "Name"
        };

        r1.Should().NotBe(r2);
    }

}
