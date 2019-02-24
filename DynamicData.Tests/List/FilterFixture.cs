using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData.Tests.Domain;
using Xunit;
using FluentAssertions;

namespace DynamicData.Tests.List
{
    
    public class FilterFixture: IDisposable
    {
        private readonly ISourceList<Person> _source;
        private readonly ChangeSetAggregator<Person> _results;

        public  FilterFixture()
        {
            _source = new SourceList<Person>();
            _results = _source.Connect(p => p.Age > 20).AsAggregator();
        }

        public void Dispose()
        {
            _source.Dispose();
            _results.Dispose();
        }

        [Fact]
        public void AddMatched()
        {
            var person = new Person("Adult1", 50);
            _source.Add(person);


            _results.MessageCount().Should().Be(2);
            _results.NumberOfAdds().Should().Be(1);
            _results.DataCount().Should().Be(1);

            _results.Items().First().Should().Be(person);
        }


        [Fact]
        public void ReplaceWithMatch()
        {
            var itemstoadd = Enumerable.Range(1, 100).Select(i => new Person("P" + i, i)).ToList();
            _source.AddRange(itemstoadd);
            _source.ReplaceAt(0, new Person("Adult1", 50));

            _results.DataCount().Should().Be(81);
        }

        [Fact]
        public void ReplaceWithNonMatch()
        {
            var itemstoadd = Enumerable.Range(1, 100).Select(i => new Person("P" + i, i)).ToList();
            _source.AddRange(itemstoadd);

            _source.ReplaceAt(50, new Person("Adult1", 1));

            _results.DataCount().Should().Be(79);
        }

        [Fact]
        public void AddRange()
        {
            var itemstoadd = Enumerable.Range(1, 100).Select(i => new Person("P" + i, i)).ToList();

            _source.AddRange(itemstoadd);

            _results.MessageCount().Should().Be(2, "Should be 1 updates");
            _results.NumberOfAdds().Should().Be(80);
            _results.DataCount().Should().Be(80, "Should be 50 item in the cache");
        }

        [Fact]
        public void Clear()
        {
            var itemstoadd = Enumerable.Range(1, 100).Select(i => new Person("P" + i, i)).ToList();

            _source.AddRange(itemstoadd);
            _source.Clear();

            _results.MessageCount().Should().Be(3);
            _results.Messages[1].First().Reason.Should().Be(ListChangeReason.AddRange);
            _results.Messages[2].First().Reason.Should().Be(ListChangeReason.Clear);
            _results.DataCount().Should().Be(0);
        }

        [Fact]
        public void AddNotMatched()
        {
            var person = new Person("Adult1", 10);
            _source.Add(person);

            //TODO: RP - This should be a count on 1?
            _results.MessageCount().Should().Be(2);
            _results.DataCount().Should().Be(0);
        }

        [Fact]
        public void AddNotMatchedAndUpdateMatched()
        {
            const string key = "Adult1";
            var notmatched = new Person(key, 19);
            var matched = new Person(key, 21);

            _source.Edit(list =>
            {
                list.Add(notmatched);
                list.Add(matched);
            });

            _results.MessageCount().Should().Be(2);
            _results.Messages[1].First().Range.First().Should().Be(matched);
            _results.Data.Items.First().Should().Be(matched);
        }

        [Fact]
        public void AttemptedRemovalOfANonExistentKeyWillBeIgnored()
        {
            _source.Remove(new Person("anyone", 1));
            _results.MessageCount().Should().Be(1);
        }

        [Fact]
        public void BatchOfUniqueUpdates()
        {
            var people = Enumerable.Range(1, 100).Select(i => new Person("Name" + i, i)).ToArray();

            _source.AddRange(people);
            _results.MessageCount().Should().Be(2);
            _results.NumberOfAdds().Should().Be(80);

            AssetItems(people);
        }

        [Fact]
        public void BatchRemoves()
        {
            var people = Enumerable.Range(1, 100).Select(l => new Person("Name" + l, l)).ToArray();
            _source.AddRange(people);
            _source.Clear();

            _results.MessageCount().Should().Be(3);
            _results.NumberOfAdds().Should().Be(80);
            _results.NumberOfRemoves().Should().Be(80);
            _results.DataCount().Should().Be(0);
        }

        [Fact]
        public void BatchSuccessiveUpdates()
        {
            Person[] people = Enumerable.Range(1, 100).Select(l => new Person("Name" + l, l)).ToArray();
            foreach (var person in people)
            {
                Person person1 = person;
                _source.Add(person1);
            }

            //TODO: THIS NEEDS FIXING
            _results.MessageCount().Should().Be(81);
            _results.DataCount().Should().Be(80);

            AssetItems(people);
        }

        private void AssetItems(IEnumerable<Person> people)
        {
            _results.Items().OrderBy(p => p.Age).ShouldAllBeEquivalentTo(people.Where(p => p.Age > 20).OrderBy(p => p.Age));
        }

        [Fact]
        public void Clear1()
        {
            var people = Enumerable.Range(1, 100).Select(l => new Person("Name" + l, l)).ToArray();
            _source.AddRange(people);
            _source.Clear();

            _results.MessageCount().Should().Be(3);
            _results.NumberOfAdds().Should().Be(80);
            _results.NumberOfRemoves().Should().Be(80);
            _results.DataCount().Should().Be(0);
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
        public void SameKeyChanges()
        {
            const string key = "Adult1";

            var toaddandremove = new Person(key, 53);
            _source.Edit(updater =>
            {
                updater.Add(new Person(key, 50));
                updater.Add(new Person(key, 52));
                updater.Add(toaddandremove);
                updater.Remove(toaddandremove);
            });

            _results.MessageCount().Should().Be(2);
            _results.NumberOfAdds().Should().Be(3);
            _results.NumberOfRemoves().Should().Be(1);
        }

        [Fact]
        public void UpdateNotMatched()
        {
            const string key = "Adult1";
            var newperson = new Person(key, 10);
            var updated = new Person(key, 11);

            _source.Add(newperson);
            _source.Add(updated);

            _results.MessageCount().Should().Be(3);
            _results.NumberOfAdds().Should().Be(0);
            _results.NumberOfRemoves().Should().Be(0);
            _results.DataCount().Should().Be(0);
        }

        [Fact]
        public void AddSubscribeRemove()
        {
            var people = Enumerable.Range(1, 100).Select(l => new Person("Name" + l, l)).ToArray();
            var source = new SourceList<Person>();
            source.AddRange(people);

            var results = source.Connect(x => x.Age > 20).AsAggregator();
            source.RemoveMany(people.Where(x => x.Age % 2 == 0));

            results.DataCount().Should().Be(40, "Should be 40 cached");
        }

    }
}
