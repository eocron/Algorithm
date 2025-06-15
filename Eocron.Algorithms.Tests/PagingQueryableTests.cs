using System;
using System.Collections.Generic;
using System.Linq;
using Eocron.Algorithms.Queryable.Paging;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class PagingQueryableTests
    {
        private List<TestDbEntity> _items;

        [SetUp]
        public void Setup()
        {
            var now = new DateTime(2017, 12, 1);
            _items = new List<TestDbEntity>()
            {
                new TestDbEntity(){Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),Name = "Test1", Modified = now.AddDays(1)},
                new TestDbEntity(){Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),Name = "Test2", Modified = now.AddDays(2)},
                new TestDbEntity(){Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),Name = "Test3", Modified = now.AddDays(3)},
                new TestDbEntity(){Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),Name = "Test4", Modified = now.AddDays(4)},
                new TestDbEntity(){Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),Name = "Test5", Modified = now.AddDays(5)},
                new TestDbEntity(){Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),Name = "Test6", Modified = now.AddDays(6)},
            };
        }

        [Test]
        public void SanityCheck()
        {
            var queryable = _items.AsQueryable();

            var cfg = new PagingConfiguration<TestDbEntity>();
            cfg.AddKeySelector(x=> x.Modified, isDescending: true);
            cfg.AddKeySelector(x=> x.Id);

            var result = new List<TestDbEntity>();
            var queries = new List<string>();
            string ct = null;
            do
            {
                var tmpQuery = queryable.ApplyContinuationTokenFilter(cfg, ct);
                queries.Add(tmpQuery.ToString());
                var tmp = tmpQuery.Take(1).ToList().FirstOrDefault();
                if (tmp != null)
                {
                    ct = cfg.GetContinuationToken(tmp);
                    result.Add(tmp);
                }
                else
                {
                    break;
                }
            } while (ct != null);

            result.Should().Equal(_items.OrderByDescending(x=> x.Modified).ThenBy(x=> x.Id));
            queries.Should().Equal([
                "System.Collections.Generic.List`1[Eocron.Algorithms.Tests.PagingQueryableTests+TestDbEntity].OrderByDescending(x => x.Modified).ThenBy(x => x.Id)",
                "System.Collections.Generic.List`1[Eocron.Algorithms.Tests.PagingQueryableTests+TestDbEntity].OrderByDescending(x => x.Modified).ThenBy(x => x.Id).Where(x => (((x.Modified == 12/7/2017 12:00:00 AM) AndAlso (x.Id > 10000000-0000-0000-0000-000000000006)) OrElse (x.Modified < 12/7/2017 12:00:00 AM)))",
                "System.Collections.Generic.List`1[Eocron.Algorithms.Tests.PagingQueryableTests+TestDbEntity].OrderByDescending(x => x.Modified).ThenBy(x => x.Id).Where(x => (((x.Modified == 12/6/2017 12:00:00 AM) AndAlso (x.Id > 10000000-0000-0000-0000-000000000005)) OrElse (x.Modified < 12/6/2017 12:00:00 AM)))",
                "System.Collections.Generic.List`1[Eocron.Algorithms.Tests.PagingQueryableTests+TestDbEntity].OrderByDescending(x => x.Modified).ThenBy(x => x.Id).Where(x => (((x.Modified == 12/5/2017 12:00:00 AM) AndAlso (x.Id > 10000000-0000-0000-0000-000000000004)) OrElse (x.Modified < 12/5/2017 12:00:00 AM)))",
                "System.Collections.Generic.List`1[Eocron.Algorithms.Tests.PagingQueryableTests+TestDbEntity].OrderByDescending(x => x.Modified).ThenBy(x => x.Id).Where(x => (((x.Modified == 12/4/2017 12:00:00 AM) AndAlso (x.Id > 10000000-0000-0000-0000-000000000003)) OrElse (x.Modified < 12/4/2017 12:00:00 AM)))",
                "System.Collections.Generic.List`1[Eocron.Algorithms.Tests.PagingQueryableTests+TestDbEntity].OrderByDescending(x => x.Modified).ThenBy(x => x.Id).Where(x => (((x.Modified == 12/3/2017 12:00:00 AM) AndAlso (x.Id > 10000000-0000-0000-0000-000000000002)) OrElse (x.Modified < 12/3/2017 12:00:00 AM)))",
                "System.Collections.Generic.List`1[Eocron.Algorithms.Tests.PagingQueryableTests+TestDbEntity].OrderByDescending(x => x.Modified).ThenBy(x => x.Id).Where(x => (((x.Modified == 12/2/2017 12:00:00 AM) AndAlso (x.Id > 10000000-0000-0000-0000-000000000001)) OrElse (x.Modified < 12/2/2017 12:00:00 AM)))"
            ]);
        }
        
        public class TestDbEntity
        {
            public Guid Id { get; set; }
            
            public string Name { get; set; }
            
            public DateTime Modified { get; set; }
        }
    }
}