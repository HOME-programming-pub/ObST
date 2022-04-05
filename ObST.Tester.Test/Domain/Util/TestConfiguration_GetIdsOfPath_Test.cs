using ObST.Core.Models;
using ObST.Tester.Domain.Util;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ObST.Tester.Test.Domain.Util;

[TestClass]
public class TestConfiguration_GetIdsOfPath_Test
{
    private static TestConfiguration DefaultConfiguration = new TestConfiguration
    {
        Paths = new PathConfigurations
        {
            {
                "api",
                new PathConfigurations
                {
                    {
                        "players",
                        new PathConfigurations
                        {
                            { "$type", "Player[]" },
                            {
                                "{Player:@id}",
                                new PathConfigurations
                                {
                                    {"$type", "Player" }
                                }
                            }
                        }
                    }
                }
            }
        }
    };

    [TestMethod]
    public void UnknownPath()
    {
        //arrange
        var config = DefaultConfiguration;

        //act
        Action act = () => config.GetIdsOfPath("/nothing", false);

        //assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void PathWithoutIds()
    {
        //arrange
        var config = DefaultConfiguration;

        //act
        var res = config.GetIdsOfPath("/api", false);

        //assert
        res.Should().BeEmpty();
    }

    [TestMethod]
    public void SingleId()
    {
        //arrange
        var config = DefaultConfiguration;

        //act
        var res = config.GetIdsOfPath("/api/players/some_weird_player_id", false);

        //assert
        res.Should().BeEquivalentTo(new[] { ("Player:@id", "some_weird_player_id") });
    }

    [TestMethod]
    public void UnmatchingCasesWithCaseSensitiveOption()
    {
        //arrange
        var config = DefaultConfiguration;

        //act
        Action act = () => config.GetIdsOfPath("/api/PlaYERS/some_weird_player_id", false);

        //assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void UnmatchingCasesWithIgnoreCaseSensitiveOption()
    {
        //arrange
        var config = DefaultConfiguration;

        //act
        var res = config.GetIdsOfPath("/api/PlaYERS/some_weird_player_id", true);

        //assert
        res.Should().BeEquivalentTo(new[] { ("Player:@id", "some_weird_player_id") });
    }
}
