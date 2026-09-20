using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Indexation.Tests;

[TestFixture]
public class InvertedIndexTests
{
    [Test]
    public void SimpleInclude()
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
    public void IncludeNonExistent()
    {
        var indexBuilder = new InvertedIndexBuilder<TestEntity>();
        indexBuilder.WithBind("userId", x=> x.UserIds);
        indexBuilder.WithBind("language", x=> x.Languages);

        var index = indexBuilder.BuildFrom(_testData);
        var result = index.Search(new SearchFilterInfo()
        {
            Include = new Dictionary<string, List<object>>()
            {
                { "userId", [666] }
            }
        }).ToList();
        result.Should().BeEmpty();
    }
    
    [Test]
    public void ExcludeNonExistent()
    {
        var indexBuilder = new InvertedIndexBuilder<TestEntity>();
        indexBuilder.WithBind("userId", x=> x.UserIds);
        indexBuilder.WithBind("language", x=> x.Languages);

        var index = indexBuilder.BuildFrom(_testData);
        var result = index.Search(new SearchFilterInfo()
        {
            Exclude = new Dictionary<string, List<object>>()
            {
                { "userId", [666] }
            }
        }).ToList();
        result.Should().Equal(_testData);
    }
    
    [Test]
    public void SimpleExclude()
    {
        var indexBuilder = new InvertedIndexBuilder<TestEntity>();
        indexBuilder.WithBind("userId", x=> x.UserIds);
        indexBuilder.WithBind("language", x=> x.Languages);

        var index = indexBuilder.BuildFrom(_testData);
        var result = index.Search(new SearchFilterInfo()
        {
            Exclude = new Dictionary<string, List<object>>()
            {
                { "userId", [3] }
            }
        }).ToList();
        result.Should().Equal([_testData[2]]);
    }
    
    [Test]
    public void SimpleIncludeAndExclude1()
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
    
    [Test]
    public void SimpleIncludeAndExclude2()
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
                { "language", ["de"] }
            }
        }).ToList();
        result.Should().Equal([_testData[0]]);
    }
    
    [Test]
    public void Empty()
    {
        var indexBuilder = new InvertedIndexBuilder<TestEntity>();
        indexBuilder.WithBind("userId", x=> x.UserIds);
        indexBuilder.WithBind("language", x=> x.Languages);

        var index = indexBuilder.BuildFrom(_testData);
        var result = index.Search(new SearchFilterInfo()).ToList();
        result.Should().BeEmpty();
    }

    [SetUp]
    public void Setup()
    {
        _testData =
        [
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
        ];
    }
    
    private TestEntity[] _testData;

    private class TestEntity
    {
        public List<int> UserIds { get; set; }
        
        public List<string> Languages { get; set; }
    }
}