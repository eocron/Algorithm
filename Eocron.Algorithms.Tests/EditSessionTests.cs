using System.Collections.Generic;
using Eocron.Algorithms.UI.Editing;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests;

[TestFixture]
public class EditSessionTests
{
    private TestDocument _document;

    [Test]
    public void Check_DeepSetAndUndo()
    {
        var session = new EditSession<TestDocument>(_document);
        session.BeginEdit();
        session.CanRedo.Should().BeFalse();
        session.CanUndo.Should().BeFalse();
        
        session.SetProperty(x=> x.Inner.Inner.Id, "3");

        session.Draft.Should().BeEquivalentTo(new TestDocument
        {
            Id = "1",
            Inner = new TestDocument
            {
                Id = "2",
                Inner = new TestDocument()
                {
                    Id = "3"
                }
            }
        });
        
        session.Undo();
        session.Draft.Should().BeEquivalentTo(new TestDocument
        {
            Id = "1",
            Inner = new TestDocument
            {
                Id = "2"
            }
        });
    }
    
    [Test]
    public void Check_SetAndUndoAndRedo()
    {
        var session = new EditSession<TestDocument>(_document);
        session.BeginEdit();
        session.SetProperty(x=> x.Id, "3");
        
        session.CanRedo.Should().BeFalse();
        session.CanUndo.Should().BeTrue();

        session.Draft.Should().BeEquivalentTo(new TestDocument
        {
            Id = "3",
            Inner = new TestDocument
            {
                Id = "2"
            }
        });
        
        session.Undo();
        session.CanRedo.Should().BeTrue();
        session.CanUndo.Should().BeFalse();
        session.Draft.Should().BeEquivalentTo(new TestDocument
        {
            Id = "1",
            Inner = new TestDocument
            {
                Id = "2"
            }
        });
        
        session.Redo();
                
        session.CanRedo.Should().BeFalse();
        session.CanUndo.Should().BeTrue();
        session.Draft.Should().BeEquivalentTo(new TestDocument
        {
            Id = "3",
            Inner = new TestDocument
            {
                Id = "2"
            }
        });
    }
    
        
    [Test]
    public void Check_SetAndUndoAndSet()
    {
        var session = new EditSession<TestDocument>(_document);
        session.BeginEdit();
        session.SetProperty(x=> x.Id, "3");
        
        session.CanRedo.Should().BeFalse();
        session.CanUndo.Should().BeTrue();

        session.Draft.Should().BeEquivalentTo(new TestDocument
        {
            Id = "3",
            Inner = new TestDocument
            {
                Id = "2"
            }
        });
        
        session.Undo();
        session.CanRedo.Should().BeTrue();
        session.CanUndo.Should().BeFalse();
        session.Draft.Should().BeEquivalentTo(new TestDocument
        {
            Id = "1",
            Inner = new TestDocument
            {
                Id = "2"
            }
        });
        
        session.SetProperty(x=> x.Id, "4");
                
        session.CanRedo.Should().BeFalse();
        session.CanUndo.Should().BeTrue();
        session.Draft.Should().BeEquivalentTo(new TestDocument
        {
            Id = "4",
            Inner = new TestDocument
            {
                Id = "2"
            }
        });
    }

    [Test]
    public void Check_InitialState()
    {
        var session = new EditSession<TestDocument>(_document);
        session.BeginEdit();
        session.CanRedo.Should().BeFalse();
        session.CanUndo.Should().BeFalse();
    }

    [SetUp]
    public void Setup()
    {
        _document = new TestDocument
        {
            Id = "1",
            Inner = new TestDocument
            {
                Id = "2",
            }
        };
    }
    
    public class TestDocument
    {
        public string Id { get; set; }
        
        public TestDocument Inner { get; set; }
        
        public List<TestDocument> Chidlren { get; set; }
    }
}