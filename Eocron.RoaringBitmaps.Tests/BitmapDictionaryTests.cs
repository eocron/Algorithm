using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.RoaringBitmaps.Tests
{
    [TestFixture]
    public class BitmapDictionaryTests
    {
        [Test]
        public void Empty()
        {
            var list = new BitmapDictionary<string>();
            list.Count.Should().Be(0);
            list.Should().BeEmpty();
        }

        [Test]
        public void Add()
        {
            var list = new BitmapDictionary<string>();
            list.AddOrUpdate("test1", 5, 2, 3, 4, 1);
            list.AddOrUpdate("test1", 8, 6);
            list.AddOrUpdate("test1", 6);
            list.AddOrUpdate("test2", 1);
            list.AddOrUpdate("test3");
            list.Count.Should().Be(2);
            list.Should().BeEquivalentTo(new[]
            {
                new KeyValuePair<string, Bitmap>("test1", new Bitmap { 1, 2, 3, 4, 5, 6, 8 }), 
                new KeyValuePair<string, Bitmap>("test2", new Bitmap { 1 })
            });
            list.TryGet("test1").Should().BeEquivalentTo(new Bitmap { 1, 2, 3, 4, 5, 6, 8 });
            list.TryGet("test2").Should().BeEquivalentTo(new Bitmap { 1 });
            list.TryGet("test3").Should().BeEquivalentTo(new Bitmap {});
        }
        
        [Test]
        public void Remove()
        {
            var list = new BitmapDictionary<string>();
            list.AddOrUpdate("test1", 5, 2, 3, 4, 1);
            list.AddOrUpdate("test1", 8, 6);
            list.AddOrUpdate("test1", 6);
            list.AddOrUpdate("test2", 1);
            list.AddOrUpdate("test3");

            list.TryRemove("test1", 8).Should().BeTrue();
            list.TryRemove("test1", new Bitmap { 6 }).Should().BeTrue();
            list.TryRemove("test2").Should().BeTrue();
            list.TryRemove("test3").Should().BeFalse();
            
            list.Count.Should().Be(1);
            list.Should().BeEquivalentTo(new[]
            {
                new KeyValuePair<string, Bitmap>("test1", new Bitmap { 1, 2, 3, 4, 5 })
            });
            list.TryGet("test1").Should().BeEquivalentTo(new Bitmap { 1, 2, 3, 4, 5 });
            list.TryGet("test2").Should().BeEquivalentTo(new Bitmap {});
            list.TryGet("test3").Should().BeEquivalentTo(new Bitmap {});
        }
    }
}