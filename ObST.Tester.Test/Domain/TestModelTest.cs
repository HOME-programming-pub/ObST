using ObST.Tester.Core.Models;
using ObST.Tester.Domain;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace ObST.Tester.Test.Domain;

[TestClass]
public class TestModelTest
{

    [TestMethod]
    public void AddSingleId_GetWithConstraint_NotMatching()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });

        var c1 = new UniqueParameter("pets:id", null!)
        {
            IsRequired = true,
        };
        var c2 = new UniqueParameter("collar:id", null!)
        {
            IsRequired = true,
        }; ;

        c1.LinkChild(c2);

        var c = new List<UniqueParameter>{
            c1,
            c2
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public void AddSingleId_GetWithConstraint()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });

        var c = new List<UniqueParameter>{
            new UniqueParameter("pets:id",null!) { IsRequired = true }
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)}
            });
    }

    [TestMethod]
    public void DeleteSingleId_GetWithConstraint()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });
        model.Delete(new[] { ("pets:id", "123") });

        var c = new List<UniqueParameter>{
            new UniqueParameter("pets:id",null!) { IsRequired = true }
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", true)}
            });
    }

    [TestMethod]
    public void AddChildrenToParent_GetWithConstraint()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });
        model.Add(new[] { ("pets:id", "123"), ("collar:id", "45") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("collar:id", null!) { IsRequired = true };

        c1.LinkChild(c2);

        var c = new List<UniqueParameter>{
            c1,
            c2
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "collar:id", ( "45", false)}
            });
    }

    [TestMethod]
    public void AddChildrenToParent_SeparatePathes_GetWithConstraint()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("collar:id", "45") });
        model.Add(new[] { ("pets:id", "124"), ("collar:id", "46") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("collar:id", null!) { IsRequired = true };

        c1.LinkChild(c2);

        var c = new List<UniqueParameter>{
            c1,
            c2
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().HaveCount(2)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "collar:id", ( "45", false)}
            })
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "124", false)},
                { "collar:id", ( "46", false)}
            });
    }

    [TestMethod]
    public void AddChildrenToParent_SameChildId()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("collar:id", "45") });
        model.Add(new[] { ("pets:id", "124"), ("collar:id", "45") });
        model.Delete(new[] { ("pets:id", "123"), ("collar:id", "45") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("collar:id", null!) { IsRequired = true };

        c1.LinkChild(c2);

        var c = new List<UniqueParameter>{
            c1,
            c2
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().HaveCount(2)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "collar:id", ( "45", true)}
            })
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "124", false)},
                { "collar:id", ( "45", false)}
            });
    }

    [TestMethod]
    public void SeparateNestedPathes_GetWithConstraint()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("house:id", "45"), ("room:id", "6") });
        model.Add(new[] { ("pets:id", "123"), ("house:id", "45"), ("room:id", "7") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("house:id", null!) { IsRequired = true };
        var c3 = new UniqueParameter("room:id", null!) { IsRequired = true };

        c1.LinkChild(c2);
        c2.LinkChild(c3);

        var c = new List<UniqueParameter>{
            c1,
            c2
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().HaveCount(2)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "house:id", ( "45", false)},
                { "room:id", ( "6", false)}
            })
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "house:id", ( "45", false)},
                { "room:id", ( "7", false)},
            });
    }

    [TestMethod]
    public void SeparatePathes_GetWithConstraint()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("house:id", "45"), ("room:id", "6") });
        model.Add(new[] { ("pets:id", "124"), ("house:id", "45"), ("room:id", "7") });
        model.Add(new[] { ("pets:id", "124"), ("house:id", "45"), ("room:id", "8") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("house:id", null!) { IsRequired = true };
        var c3 = new UniqueParameter("room:id", null!) { IsRequired = true };

        c1.LinkChild(c2);
        c2.LinkChild(c3);

        var c = new List<UniqueParameter>{
            c1,
            c2
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().HaveCount(3)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "house:id", ( "45", false)},
                { "room:id", ( "6", false)}
            })
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "124", false)},
                { "house:id", ( "45", false)},
                { "room:id", ( "7", false)},
            })
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "124", false)},
                { "house:id", ( "45", false)},
                { "room:id", ( "8", false)},
            });
    }

    [TestMethod]
    public void MultiparentsWithDifferentMappings()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("room:id", "6") });
        model.Add(new[] { ("house:id", "45"), ("room:id", "6") });

        var c11 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c12 = new UniqueParameter("room:id", null!) { IsRequired = true };

        var c21 = new UniqueParameter("house:id", null!) { IsRequired = true };
        var c22 = new UniqueParameter("room:id", null!) { IsRequired = true };


        c11.LinkChild(c12);
        c21.LinkChild(c22);

        var actual1 = model.GetMatchingIdSubset(new List<UniqueParameter>{
            c11,
            c12
        });

        var actual2 = model.GetMatchingIdSubset(new List<UniqueParameter>{
            c21,
            c22
        });

        actual1.Should().HaveCount(1)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "room:id", ( "6", false)}
            });

        actual2.Should().HaveCount(1)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "house:id", ( "45", false)},
                { "room:id", ( "6", false)},
            });
    }

    [TestMethod]
    public void MultiparentsWithDifferentMappings_DeleteOneParent()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("room:id", "6") });
        model.Add(new[] { ("house:id", "45"), ("room:id", "6") });
        model.Delete(new[] { ("pets:id", "123") });

        var c11 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c12 = new UniqueParameter("room:id", null!) { IsRequired = true };

        var c21 = new UniqueParameter("house:id", null!) { IsRequired = true };
        var c22 = new UniqueParameter("room:id", null!) { IsRequired = true };


        c11.LinkChild(c12);
        c21.LinkChild(c22);

        var actual1 = model.GetMatchingIdSubset(new List<UniqueParameter>{
            c11,
            c12
        });

        var actual2 = model.GetMatchingIdSubset(new List<UniqueParameter>{
            c21,
            c22
        });

        actual1.Should().HaveCount(1)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", true)},
                { "room:id", ( "6", true)}
            });

        actual2.Should().HaveCount(1)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "house:id", ( "45", false)},
                { "room:id", ( "6", true)},
            });
    }

    [TestMethod]
    public void MultiparentsWithDifferentMappingsAndNestedPath()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("room:id", "6") });
        model.Add(new[] { ("house:id", "45"), ("room:id", "6"), ("window:id", "5") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("room:id", null!) { IsRequired = true };
        var c3 = new UniqueParameter("window:id", null!) { IsRequired = true };


        c1.LinkChild(c2);
        c2.LinkChild(c3);

        var actual = model.GetMatchingIdSubset(new List<UniqueParameter>{
            c1,
            c2,
            c3
        });

        actual.Should().HaveCount(1)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "room:id", ( "6", false)},
                { "window:id", ( "5", false)}
            });
    }

    [TestMethod]
    public void GetWithDistinctConstrains()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });
        model.Add(new[] { ("pets:id", "123"), ("collar:id", "45") });
        model.Add(new[] { ("human:id", "human-Id") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("collar:id", null!) { IsRequired = true };
        var c3 = new UniqueParameter("human:id", null!) { IsRequired = true };

        c1.LinkChild(c2);

        var c = new List<UniqueParameter>{
            c1,
            c2,
            c3
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "collar:id", ( "45", false)},
                { "human:id", ( "human-Id", false)},
            });
    }


    [TestMethod]
    public void GetWithDistinctConstrains_MultipleEntries()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });
        model.Add(new[] { ("pets:id", "123"), ("collar:id", "45") });
        model.Add(new[] { ("human:id", "human-Id1") });
        model.Add(new[] { ("human:id", "human-Id2") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("collar:id", null!) { IsRequired = true };
        var c3 = new UniqueParameter("human:id", null!) { IsRequired = true };

        c1.LinkChild(c2);

        var c = new List<UniqueParameter>{
            c1,
            c2,
            c3
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().HaveCount(2)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "collar:id", ( "45", false)},
                { "human:id", ( "human-Id1", false)},
            })
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "collar:id", ( "45", false)},
                { "human:id", ( "human-Id2", false)},
            });
    }

    [TestMethod]
    public void GetWithDoubleConstrains()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });
        model.Add(new[] { ("pets:id", "123"), ("collar:id", "45") });
        model.Add(new[] { ("pets:id", "123"), ("house:id", "a") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("collar:id", null!) { IsRequired = true };
        var c3 = new UniqueParameter("house:id", null!) { IsRequired = true };

        c1.LinkChild(c2);
        c1.LinkChild(c3);

        var c = new List<UniqueParameter>{
            c1,
            c2,
            c3
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().HaveCount(1)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "collar:id", ( "45", false)},
                { "house:id", ( "a", false)},
            });
    }
    [TestMethod]
    public void GetWithDoubleConstrains_AdditionalEntryWithoutRelation()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });
        model.Add(new[] { ("pets:id", "123"), ("collar:id", "45") });
        model.Add(new[] { ("pets:id", "123"), ("house:id", "a") });
        model.Add(new[] { ("house:id", "b") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("collar:id", null!) { IsRequired = true };
        var c3 = new UniqueParameter("house:id", null!) { IsRequired = true };

        c1.LinkChild(c2);
        c1.LinkChild(c3);

        var c = new List<UniqueParameter>{
            c1,
            c2,
            c3
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().HaveCount(1)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "collar:id", ( "45", false)},
                { "house:id", ( "a", false)},
            });
    }

    [TestMethod]
    public void Get_SingleKey()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });

        var actual = model.Get(Enumerable.Empty<(string, string)>(), "pets:id");

        actual.Should().ContainSingle()
            .Which.Should().Be("123");
    }

    [TestMethod]
    public void GetDeleted_SingleKey_NotDeleted()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });

        var actual = model.GetDeleted(Enumerable.Empty<(string, string)>(), "pets:id");

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public void GetAll_SingleKey()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });

        var actual = model.Get(Enumerable.Empty<(string, string)>(), "pets:id", includeDeleted: true);

        actual.Should().ContainSingle()
            .Which.Should().Be("123");
    }

    [TestMethod]
    public void Get_SingleKey_Deleted()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });
        model.Delete(new[] { ("pets:id", "123") });

        var actual = model.Get(Enumerable.Empty<(string, string)>(), "pets:id");

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public void GetDeleted_SingleKey_Deleted()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });
        model.Delete(new[] { ("pets:id", "123") });

        var actual = model.GetDeleted(Enumerable.Empty<(string, string)>(), "pets:id");

        actual.Should().ContainSingle()
            .Which.Should().Be("123");
    }

    [TestMethod]
    public void GetAll_SingleKey_Deleted()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });
        model.Delete(new[] { ("pets:id", "123") });

        var actual = model.Get(Enumerable.Empty<(string, string)>(), "pets:id", includeDeleted: true);

        actual.Should().ContainSingle()
            .Which.Should().Be("123");
    }

    [TestMethod]
    public void Get_NestedKey()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("collar:id", "4") });
        model.Add(new[] { ("pets:id", "123"), ("other:id", "9") });
        model.Add(new[] { ("pets:id", "456"), ("collar:id", "12") });

        var actual = model.Get(new[] { ("pets:id", "123") }, "collar:id");

        actual.Should().BeEquivalentTo(new[] { "4" });
    }

    [TestMethod]
    public void Get_NestedKey_AsRoot()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("collar:id", "4") });
        model.Add(new[] { ("pets:id", "123"), ("other:id", "9") });
        model.Add(new[] { ("pets:id", "456"), ("collar:id", "12") });

        var actual = model.Get(Enumerable.Empty<(string, string)>(), "collar:id");

        actual.Should().BeEquivalentTo(new[] { "4", "12" });
    }

    [TestMethod]
    public void MultiplePathsWithParentConstraint()
    {
        var c1 = new UniqueParameter("main:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("other:id", null!) { IsRequired = true };

        c1.LinkChild(c2);

        var constrains = new List<UniqueParameter>
        {
            c1,
            c2
        };


        var model = new TestModel();
        model.Add(new[] { ("main:id", "1"), ("other:id", "4") });
        model.Add(new[] { ("main:id", "2") });
        model.Add(new[] { ("main:id", "3"), ("other:id", "6") });

        var actual = model.GetMatchingIdSubset(constrains);

        actual.Should().HaveCount(2)
            .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "main:id", ("1", false) },
                { "other:id", ("4", false) }
            })
                .And.ContainEquivalentOf(new Dictionary<string, (string, bool)>
            {
                { "main:id", ("3", false) },
                { "other:id", ("6", false) }
            });
    }


    [TestMethod]
    public void GetSingleOptional()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123") });

        var c = new List<UniqueParameter>{
            new UniqueParameter("pets:id",null!) { IsRequired = false }
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().BeEquivalentTo(new List<Dictionary<string, (string?, bool)>>
        {
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", ("123", false)},
            },
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", (null, false)}
            }
        });
    }

    [TestMethod]
    public void GetSingleOptionalOfEmptyModel()
    {
        var model = new TestModel();

        var c = new List<UniqueParameter>{
            new UniqueParameter("pets:id",null!) { IsRequired = false }
        };

        var actual = model.GetMatchingIdSubset(c);

        actual.Should().BeEquivalentTo(new List<Dictionary<string, (string?, bool)>>
        {
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", (null, false)}
            }
        });
    }

    [TestMethod]
    public void GetSingleOptionalAndRequiredParent()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("collars:id", "444") });
        model.Add(new[] { ("pets:id", "234") });
        model.Add(new[] { ("collars:id", "333") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("collars:id", null!) { IsRequired = false };

        c1.LinkChild(c2);


        var actual = model.GetMatchingIdSubset(new List<UniqueParameter> { c1, c2 });

        actual.Should().BeEquivalentTo(new List<Dictionary<string, (string?, bool)>>
        {
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", ("123", false)},
                { "collars:id", ("444", false)}
            },
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", ("234", false)},
                { "collars:id", (null, false)}
            },
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", ("123", false)},
                { "collars:id", (null, false)}
            }
        });
    }

    [TestMethod]
    public void GetSingleOptionalAndRequiredChild()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("collars:id", "444") });
        model.Add(new[] { ("pets:id", "234") });
        model.Add(new[] { ("collars:id", "333") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = false };
        var c2 = new UniqueParameter("collars:id", null!) { IsRequired = true };

        c1.LinkChild(c2);


        var actual = model.GetMatchingIdSubset(new List<UniqueParameter> { c1, c2 });

        actual.Should().BeEquivalentTo(new List<Dictionary<string, (string, bool)>>
        {
            new Dictionary<string, (string, bool)>
            {
                { "pets:id", ( "123", false)},
                { "collars:id", ( "444", false)}
            }
        });
    }

    [TestMethod]
    public void GetSingleOptionalWithSingleRequired()
    {
        var model = new TestModel();

        model.Add(new[] { ("pets:id", "123"), ("collars:id", "444") });
        model.Add(new[] { ("pets:id", "234") });
        model.Add(new[] { ("collars:id", "333") });

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = false };
        var c2 = new UniqueParameter("collars:id", null!) { IsRequired = true };

        var actual = model.GetMatchingIdSubset(new List<UniqueParameter> { c1, c2 });

        actual.Should().BeEquivalentTo(new List<Dictionary<string, (string?, bool)>>
        {
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", ( "123", false)},
                { "collars:id", ( "444", false)}
            },
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", ( null, false)},
                { "collars:id", ( "444", false)}
            },
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", ( "234", false)},
                { "collars:id", ( "444", false)}
            },
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", ( "123", false)},
                { "collars:id", ( "333", false)}
            },
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", ( null, false)},
                { "collars:id", ( "333", false)}
            },
            new Dictionary<string, (string?, bool)>
            {
                { "pets:id", ( "234", false)},
                { "collars:id", ( "333", false)}
            }
        });
    }

    [TestMethod]
    public void GetSingleOptionalWithSingleRequiredOnEmptyModel()
    {
        var model = new TestModel();

        var c1 = new UniqueParameter("pets:id", null!) { IsRequired = false };
        var c2 = new UniqueParameter("collars:id", null!) { IsRequired = true };

        var actual = model.GetMatchingIdSubset(new List<UniqueParameter> { c1, c2 });

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public void AddChildToNestedObject()
    {
        var model = new TestModel();

        model.Add(new[] { ("project:id", "1"), ("branch:id", "master") });
        model.Add(new[] { ("project:id", "1"), ("branch:id", "master"), ("commit:id", "1234") });
        model.Add(new[] { ("project:id", "2"), ("branch:id", "master") });
        model.Add(new[] { ("project:id", "2"), ("branch:id", "master"), ("commit:id", "2345") });

        var c1 = new UniqueParameter("project:id", null!) { IsRequired = true };
        var c2 = new UniqueParameter("branch:id", null!) { IsRequired = true };
        var c3 = new UniqueParameter("commit:id", null!) { IsRequired = true };

        c1.LinkChild(c2);
        c2.LinkChild(c3);

        var actual = model.GetMatchingIdSubset(new List<UniqueParameter> { c1, c2, c3 });

        actual.Should().BeEquivalentTo(new List<Dictionary<string, (string, bool)>>
        {
            new Dictionary<string, (string, bool)>
            {
                { "project:id", ( "1", false)},
                { "branch:id", ( "master", false)},
                { "commit:id", ( "1234", false)}
            },
                new Dictionary<string, (string, bool)>
            {
                { "project:id", ( "2", false)},
                { "branch:id", ( "master", false)},
                { "commit:id", ( "2345", false)}
            }
        });
    }
}
