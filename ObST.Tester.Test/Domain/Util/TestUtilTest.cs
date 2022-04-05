using ObST.Tester.Domain;
using ObST.Tester.Domain.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ObST.Tester.Test.Domain.Util;


[TestClass]
public class TestUtilTest
{

    [TestMethod]
    public void GetPlainObjectId()
    {
        var testModel = new Mock<TestModel>();

        var obj = JToken.FromObject(new
        {
            id = "123"
        });

        var schema = NJsonSchema.JsonSchema.FromJsonAsync(@"
            {
                ""type"": ""object"",
                ""title"": ""House"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""title"": ""House:@id""
                    }
                }
            }").Result;


        testModel.Object.SearchForIds(obj, schema, Enumerable.Empty<(string, string)>());

        var expected = new List<(string, string)> { ("House:@id", "123") };

        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected))), Times.Once);
        testModel.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void GetPlainObjectIdFromArray()
    {
        var testModel = new Mock<TestModel>();

        var obj = JToken.FromObject(new[]{
                new
                {
                    id = "123"
                }
            });

        var schema = NJsonSchema.JsonSchema.FromJsonAsync(@"
            {
                ""type"": ""array"",
                ""items"": {
                    ""type"": ""object"",
                    ""title"": ""House"",
                    ""properties"": {
                        ""id"": {
                            ""type"": ""string"",
                            ""title"": ""House:@id""
                        }
                    }
                }
            }").Result;


        testModel.Object.SearchForIds(obj, schema, Enumerable.Empty<(string, string)>());

        var expected = new List<(string, string)> { ("House:@id", "123") };

        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected))), Times.Once);
        testModel.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void GetPlainObjectIdWithPathVariables()
    {
        var testModel = new Mock<TestModel>();

        var obj = JToken.FromObject(new
        {
            id = "123"
        });

        var schema = NJsonSchema.JsonSchema.FromJsonAsync(@"
            {
                ""type"": ""object"",
                ""title"": ""House"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""title"": ""House:@id""
                    }
                }
            }").Result;


        testModel.Object.SearchForIds(obj, schema, new List<(string, string)> { ("City:@id", "456") });

        var expected = new List<(string, string)> { ("City:@id", "456"), ("House:@id", "123") };

        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected))), Times.Once);
        testModel.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void GetPlainObjectIdWithPathVariables_SelfCall()
    {
        var testModel = new Mock<TestModel>();

        var obj = JToken.FromObject(new
        {
            id = "123"
        });

        var schema = NJsonSchema.JsonSchema.FromJsonAsync(@"
            {
                ""type"": ""object"",
                ""title"": ""House"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""title"": ""House:@id""
                    }
                }
            }").Result;

        testModel.Object.SearchForIds(obj, schema, new List<(string, string)> { ("City:@id", "456"), ("House:@id", "123") });

        var expected = new List<(string, string)> { ("City:@id", "456"), ("House:@id", "123") };

        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected))), Times.Once);
        testModel.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void GetPlainObjectIdWithPathVariables_NestedCall()
    {
        var testModel = new Mock<TestModel>();

        var obj = JToken.FromObject(new
        {
            id = "123"
        });

        var schema = NJsonSchema.JsonSchema.FromJsonAsync(@"
            {
                ""type"": ""object"",
                ""title"": ""House"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""title"": ""House:@id""
                    }
                }
            }").Result;

        testModel.Object.SearchForIds(obj, schema, new List<(string, string)> { ("City:@id", "456"), ("House:@id", "456") });

        var expected = new List<(string, string)> { ("City:@id", "456"), ("House:@id", "456"), ("House:@id", "123") };

        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected))), Times.Once);
        testModel.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void GetNestedArrayChildObjectId()
    {
        var testModel = new Mock<TestModel>();

        var obj = JToken.FromObject(new
        {
            id = "123",
            rooms = new[]
            {
                    new
                    {
                        id = "6"
                    },
                    new
                    {
                        id = "7"
                    }
                }
        });

        var schema = NJsonSchema.JsonSchema.FromJsonAsync(@"
            {
                ""type"": ""object"",
                ""title"": ""House"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""title"": ""House:@id""
                    },
                    ""rooms"": {
                        ""type"": ""array"",
                        ""title"": ""<Room"",
                        ""items"": {
                            ""type"": ""object"",
                            ""title"": ""<Room"",
                            ""properties"": {
                                ""id"": {
                                    ""type"": ""string"",
                                    ""title"": ""Room:@id""
                                }
                            }
                        }
                    }
                }
            }").Result;


        testModel.Object.SearchForIds(obj, schema, Enumerable.Empty<(string, string)>());

        var expected = new[]
        {
                new List<(string, string)> { ("House:@id", "123") },
                new List<(string, string)> { ("House:@id", "123"), ("Room:@id", "6") },
                new List<(string, string)> { ("House:@id", "123"), ("Room:@id", "7") },
            };

        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[0]))), Times.Once);
        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[1]))), Times.Once);
        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[2]))), Times.Once);
        testModel.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void GetNestedParentObjectId()
    {
        var testModel = new Mock<TestModel>();

        var obj = JToken.FromObject(new
        {
            id = "6",
            house = new
            {
                id = "123"
            }
        });

        var schema = NJsonSchema.JsonSchema.FromJsonAsync(@"
            {
                ""type"": ""object"",
                ""title"": ""Room"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""title"": ""Room:@id""
                    },
                    ""house"": {
                        ""type"": ""object"",
                        ""title"": "">House"",
                        ""properties"": {
                            ""id"": {
                                ""type"": ""string"",
                                ""title"": ""House:@id""
                            }
                        }
                    }
                }
            }").Result;


        testModel.Object.SearchForIds(obj, schema, Enumerable.Empty<(string, string)>());

        var expected = new[]
        {
                new List<(string, string)> { ("Room:@id", "6") },
                new List<(string, string)> { ("House:@id", "123"), ("Room:@id", "6") }
            };

        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[0]))), Times.Once);
        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[1]))), Times.Once);
        testModel.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void GetNestedParentObjectWithNestedReferenceId()
    {
        var testModel = new Mock<TestModel>();

        var obj = JToken.FromObject(new
        {
            id = "6",
            house = new
            {
                id = "123",
                residentIds = new[] { "3", "4" }
            }
        });

        var schema = NJsonSchema.JsonSchema.FromJsonAsync(@"
            {
                ""type"": ""object"",
                ""title"": ""Room"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""title"": ""Room:@id""
                    },
                    ""house"": {
                        ""type"": ""object"",
                        ""title"": "">House"",
                        ""properties"": {
                            ""id"": {
                                ""type"": ""string"",
                                ""title"": ""House:@id""
                            },
                            ""residentIds"": {
                                ""type"": ""array"",
                                ""title"": ""<Resident:@id"",
                                ""items"": {
                                    ""type"": ""string"",
                                    ""title"": ""<Resident:@id""
                                }
                            }
                        }
                    }
                }
            }").Result;


        testModel.Object.SearchForIds(obj, schema, Enumerable.Empty<(string, string)>());

        var expected = new[]
        {
                new List<(string, string)> { ("Room:@id", "6") },
                new List<(string, string)> { ("House:@id", "123"), ("Room:@id", "6") },
                new List<(string, string)> { ("House:@id", "123"), ("Resident:@id", "3") },
                new List<(string, string)> { ("House:@id", "123"), ("Resident:@id", "4") }
            };

        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[0]))), Times.Once);
        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[1]))), Times.Once);
        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[2]))), Times.Once);
        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[3]))), Times.Once);
        testModel.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void GetReferencedParentId()
    {
        var testModel = new Mock<TestModel>();

        var obj = JToken.FromObject(new
        {
            id = "6",
            houseId = "123"
        });

        var schema = NJsonSchema.JsonSchema.FromJsonAsync(@"
            {
                ""type"": ""object"",
                ""title"": ""Room"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""title"": ""Room:@id""
                    },
                    ""houseId"": {
                        ""type"": ""string"",
                        ""title"": "">House:@id""
                    }
                }
            }").Result;


        testModel.Object.SearchForIds(obj, schema, Enumerable.Empty<(string, string)>());

        var expected = new[]
        {
                new List<(string, string)> { ("Room:@id", "6") },
                new List<(string, string)> { ("House:@id", "123"), ("Room:@id", "6") }
            };

        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[0]))), Times.Once);
        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[1]))), Times.Once);
        testModel.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void GetReferencedChildId()
    {
        var testModel = new Mock<TestModel>();

        var obj = JToken.FromObject(new
        {
            id = "123",
            roomId = "6"
        });

        var schema = NJsonSchema.JsonSchema.FromJsonAsync(@"
            {
                ""type"": ""object"",
                ""title"": ""House"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""title"": ""House:@id""
                    },
                    ""roomId"": {
                        ""type"": ""string"",
                        ""title"": ""<Room:@id""
                    }
                }
            }").Result;


        testModel.Object.SearchForIds(obj, schema, Enumerable.Empty<(string, string)>());

        var expected = new[]
        {
                new List<(string, string)> { ("House:@id", "123") },
                new List<(string, string)> { ("House:@id", "123"), ("Room:@id", "6") }
            };

        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[0]))), Times.Once);
        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[1]))), Times.Once);
        testModel.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void GetUnspecifiedReferencedId()
    {
        var testModel = new Mock<TestModel>();

        var obj = JToken.FromObject(new
        {
            id = "123",
            roomId = "6"
        });

        var schema = NJsonSchema.JsonSchema.FromJsonAsync(@"
            {
                ""type"": ""object"",
                ""title"": ""House"",
                ""properties"": {
                    ""id"": {
                        ""type"": ""string"",
                        ""title"": ""House:@id""
                    },
                    ""roomId"": {
                        ""type"": ""string"",
                        ""title"": ""Room:@id""
                    }
                }
            }").Result;


        testModel.Object.SearchForIds(obj, schema, Enumerable.Empty<(string, string)>());

        var expected = new[]
        {
                new List<(string, string)> { ("House:@id", "123") },
                new List<(string, string)> { ("Room:@id", "6") }
            };

        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[0]))), Times.Once);
        testModel.Verify(c => c.Add(It.Is<IEnumerable<(string, string)>>(actual => actual.SequenceEqual(expected[1]))), Times.Once);
        testModel.VerifyNoOtherCalls();
    }
}