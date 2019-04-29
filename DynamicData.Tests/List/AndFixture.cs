using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DynamicData.Tests.List
{

    public class AndBug
    {

        [Fact]
        public void Cache()
        {
            var list1 = new SourceCache<int, int>(i => i);
            var list2 = new SourceCache<int, int>(i => i);

            list1.AddOrUpdate(1);
            list1.AddOrUpdate(2);
            list1.AddOrUpdate(3);

            list2.AddOrUpdate(3);
            list2.AddOrUpdate(4);
            list2.AddOrUpdate(5);

            var and = list1.Connect().And(list2.Connect()).AsObservableCache();

            and.Connect().ToCollection().Subscribe((l) =>
            {
                Console.WriteLine($"[{string.Join(", ", l)}]"); // Produces "[1, 2, 3]", expected "[3]"
            });
        }

        [Fact]
        public void List()
        {
            var list1 = new SourceList<int>();
            var list2 = new SourceList<int>();

            list1.Add(1);
            list1.Add(2);
            list1.Add(3);

            list2.Add(3);
            list2.Add(4);
            list2.Add(5);

            var and = list1.Connect().And(list2.Connect()).AsObservableList();

            and.Connect().ToCollection().Subscribe((l) =>
            {
                Console.WriteLine($"[{string.Join(", ", l)}]"); // Produces "[1, 2, 3]", expected "[3]"
            });

            list1.Add(4);
            list2.Remove(3);
        }

    }


    public class AndFixture : AndFixtureBase
    {
        protected override IObservable<IChangeSet<int>> CreateObservable()
        {
            return _source1.Connect().And(_source2.Connect());
        }
    }

    
    public class AndCollectionFixture : AndFixtureBase
    {
        protected override IObservable<IChangeSet<int>> CreateObservable()
        {
            var l = new List<IObservable<IChangeSet<int>>> { _source1.Connect(), _source2.Connect() };
            return l.And();
        }
    }

    
    public abstract class AndFixtureBase: IDisposable
    {
        protected ISourceList<int> _source1;
        protected ISourceList<int> _source2;
        private readonly ChangeSetAggregator<int> _results;


        protected AndFixtureBase()
        {
            _source1 = new SourceList<int>();
            _source2 = new SourceList<int>();
            _results = CreateObservable().AsAggregator();
        }

        protected abstract IObservable<IChangeSet<int>> CreateObservable();

        public void Dispose()
        {
            _source1.Dispose();
            _source2.Dispose();
            _results.Dispose();
        }

        [Fact]
        public void ExcludedWhenItemIsInOneSource()
        {
            _source1.Add(1);
            _results.Data.Count.Should().Be(0);
        }

        [Fact]
        public void IncludedWhenItemIsInTwoSources()
        {
            _source1.Add(1);
            _source2.Add(1);
            _results.Data.Count.Should().Be(1);
        }

        [Fact]
        public void RemovedWhenNoLongerInBoth()
        {
            _source1.Add(1);
            _source2.Add(1);
            _source1.Remove(1);
            _results.Data.Count.Should().Be(0);
        }

        [Fact]
        public void CombineRange()
        {
            _source1.AddRange(Enumerable.Range(1, 10));
            _source2.AddRange(Enumerable.Range(6, 10));
            _results.Data.Count.Should().Be(5);
            _results.Data.Items.ShouldAllBeEquivalentTo(Enumerable.Range(6, 5));
        }

        [Fact]
        public void ClearOneClearsResult()
        {
            _source1.AddRange(Enumerable.Range(1, 5));
            _source2.AddRange(Enumerable.Range(1, 5));
            _source1.Clear();
            _results.Data.Count.Should().Be(0);
        }
    }
}
