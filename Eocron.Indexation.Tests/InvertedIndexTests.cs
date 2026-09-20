using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Indexation.Tests;

[TestFixture]
public class InvertedIndexTests
{
    private TestEntity[] _testData;

    [SetUp]
    public void Setup()
    {
        _testData = new TestEntity[]
        {
            new()
            {
                UserIds = [1, 2, 3],
                Languages = ["ru", "en"]
            },
            new()
            {
                UserIds = [3, 4, 5],
                Languages = ["en", "de"],
            },
            new()
            {
                UserIds = [6, 7, 8],
                Languages = ["zn", "tr"],
            }
        };
    }

    [Test]
    public void IntersectionOfData()
    {
        var indexBuilder = new InvertedIndexBuilder<TestEntity>();
        indexBuilder.WithBind("userId", x=> x.UserIds);
        indexBuilder.WithBind("language", x=> x.Languages);

        var index = indexBuilder.BuildFrom(_testData);
        var result = index.Search(new SearchFilterInfo()
        {
            Include = new Dictionary<string, List<object>>()
            {
                { "userId", [3] }
            }
        }).ToList();
        result.Should().Equal([_testData[0], _testData[1]]);
    }
    
    [Test]
    public void IntersectionOfDataExcludeOne()
    {
        var indexBuilder = new InvertedIndexBuilder<TestEntity>();
        indexBuilder.WithBind("userId", x=> x.UserIds);
        indexBuilder.WithBind("language", x=> x.Languages);

        var index = indexBuilder.BuildFrom(_testData);
        var result = index.Search(new SearchFilterInfo()
        {
            Include = new Dictionary<string, List<object>>()
            {
                { "userId", [3] }
            },
            Exclude = new Dictionary<string, List<object>>()
            {
                { "userId", [5] }
            }
        }).ToList();
        result.Should().Equal([_testData[0]]);
    }
    
    public class TestEntity
    {
        public List<int> UserIds { get; set; }
        
        public List<string> Languages { get; set; }
    }
    
    public class TestRequest
    {
        public int UserId { get; set; }
        
        public string Language { get; set; }
    }
}