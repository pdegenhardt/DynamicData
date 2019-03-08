using System;
using System.Linq;
using DynamicData.List.Tests;
using DynamicData.Tests.Domain;
using FluentAssertions;
using Xunit;

namespace DynamicData.Tests.List
{
    
    public class TransformFixture: IDisposable
    {
        private readonly ISourceList<Person> _source;
        private readonly ChangeSetAggregator<PersonWithGender> _results;

        private readonly Func<Person, PersonWithGender> _transformFactory = p =>
        {
            string gender = p.Age % 2 == 0 ? "M" : "F";
            return new PersonWithGender(p, gender);
        };

        public  TransformFixture()
        {
            _source = new SourceList<Person>();
            _results = new ChangeSetAggregator<PersonWithGender>(_source.Connect().Transform(_transformFactory));
        }

        public void Dispose()
        {
            _source.Dispose();
            _results.Dispose();
        }

        [Fact]
        public void Add()
        {
            var person = new Person("Adult1", 50);
            _source.Add(person);

            _results.MessageCount().Should().Be(2);
            _results.DataCount().Should().Be(1);
            _results.Data.Items.First().Should().Be(_transformFactory(person));
        }

        [Fact]
        public void Remove()
        {
            const string key = "Adult1";
            var person = new Person(key, 50);

            _source.Add(person);
            _source.Remove(person);

            _results.MessageCount().Should().Be(3);
            _results.NumberOfAdds().Should().Be(1);
            _results.NumberOfRemoves().Should().Be(1);
            _results.DataCount().Should().Be(0);
        }

        [Fact]
        public void RemoveWithoutIndex()
        {
            const string key = "Adult1";
            var person = new Person(key, 50);

            var results = _source.Connect().RemoveIndex().Transform(_transformFactory).AsAggregator();

            _source.Add(person);
            _source.Remove(person);

            results.MessageCount().Should().Be(3);
            results.NumberOfAdds().Should().Be(1);
            results.NumberOfRemoves().Should().Be(1);
            results.DataCount().Should().Be(0);
        }

        [Fact]
        public void Update()
        {
            const string key = "Adult1";
            var newperson = new Person(key, 50);
            var updated = new Person(key, 51);

            _source.Add(newperson);
            _source.Add(updated);

            _results.MessageCount().Should().Be(3);
            _results.NumberOfAdds().Should().Be(2);
            _results.DataCount().Should().Be(2);
        }

        [Fact]
        public void BatchOfUniqueUpdates()
        {
            var people = Enumerable.Range(1, 100).Select(i => new Person("Name" + i, i)).ToArray();

            _source.AddRange(people);

            _results.MessageCount().Should().Be(2);
            _results.NumberOfAdds().Should().Be(100);
            _results.DataCount().Should().Be(100);

            _results.Items().OrderBy(p => p.Age).ShouldAllBeEquivalentTo(_results.Data.Items.OrderBy(p => p.Age));
        }

        [Fact]
        public void SameKeyChanges()
        {
            var people = Enumerable.Range(1, 10).Select(i => new Person("Name", i)).ToArray();

            _source.AddRange(people);

            _results.MessageCount().Should().Be(2);
            _results.NumberOfAdds().Should().Be(10);
            _results.DataCount().Should().Be(10);
        }

        [Fact]
        public void Clear()
        {
            var people = Enumerable.Range(1, 10).Select(l => new Person("Name" + l, l)).ToArray();

            _source.AddRange(people);
            _source.Clear();

            _results.MessageCount().Should().Be(3);
            _results.NumberOfAdds().Should().Be(10);
            _results.NumberOfRemoves().Should().Be(10);
            _results.DataCount().Should().Be(0);
        }
    }
}
