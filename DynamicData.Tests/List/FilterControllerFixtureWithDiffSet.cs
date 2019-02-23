using System;
using System.Linq;
using System.Reactive.Subjects;
using DynamicData.Tests.Domain;
using Xunit;
using FluentAssertions;

namespace DynamicData.Tests.List
{
    
    public class FilterControllerFixtureWithDiffSet : IDisposable
    {
        private readonly ISourceList<Person> _source;
        private readonly ChangeSetAggregator<Person> _results;
        private readonly ISubject<Func<Person, bool>> _filter;

        public  FilterControllerFixtureWithDiffSet()
        {
            _source = new SourceList<Person>();
            _filter = new BehaviorSubject<Func<Person, bool>>(p => p.Age > 20);
            _results = _source.Connect().Filter(_filter).AsAggregator();
        }

        public void Dispose()
        {
            _source.Dispose();
            _results.Dispose();
        }

        [Fact]
        public void ChangeFilter()
        {
            var people = Enumerable.Range(1, 100).Select(i => new Person("P" + i, i)).ToList();

            _source.AddRange(people);
            _results.DataCount().Should().Be(80);

            _filter.OnNext(p => p.Age <= 50);
            _results.DataCount().Should().Be(50);
            _results.MessageCount().Should().Be(3);

            _results.Data.Items.All(p => p.Age <= 50).Should().BeTrue();
        }

        [Fact]
        public void ReevaluateFilter()
        {
            //re-evaluate for inline changes
            var people = Enumerable.Range(1, 100).Select(i => new Person("P" + i, i)).ToArray();

            _source.AddRange(people);
            _results.DataCount().Should().Be(80);

            foreach (var person in people)
            {
                person.Age = person.Age + 10;
            }
            _filter.OnNext(p => p.Age > 20);

            _results.DataCount().Should().Be(90);
            _results.MessageCount().Should().Be(3);

            foreach (var person in people)
            {
                person.Age = person.Age - 10;
            }
            _filter.OnNext(p => p.Age > 20);

            _results.DataCount().Should().Be(80);
            _results.MessageCount().Should().Be(4);
        }

        #region Static filter tests

        /* Should be the same as standard lambda filter */

        [Fact]
        public void AddMatched()
        {
            var person = new Person("Adult1", 50);
            _source.Add(person);

            _results.MessageCount().Should().Be(2);
            _results.DataCount().Should().Be(1);
            _results.Data.Items.First().Should().Be(person);
        }

        [Fact]
        public void AddNotMatched()
        {
            var person = new Person("Adult1", 10);
            _source.Add(person);

            _results.MessageCount().Should().Be(1);
            _results.TotalChangeCount().Should().Be(0);
            _results.DataCount().Should().Be(0);
        }

        [Fact]
        public void AddNotMatchedAndUpdateMatched()
        {
            const string key = "Adult1";
            var notmatched = new Person(key, 19);
            var matched = new Person(key, 21);

            _source.Edit(updater =>
            {
                updater.Add(notmatched);
                updater.Add(matched);
            });

            _results.MessageCount().Should().Be(2);
            _results.DataCount().Should().Be(1);

            _results.Messages[1].First().Range.First().Should().Be(matched, "Should be same person");
            _results.Data.Items.First().Should().Be(matched, "Should be same person");
        }

        [Fact]
        public void AttemptedRemovalOfANonExistentKeyWillBeIgnored()
        {
            _source.Remove(new Person("A", 1));
            _results.MessageCount().Should().Be(1, "Should be 0 updates");
        }

        [Fact]
        public void BatchOfUniqueUpdates()
        {
            var people = Enumerable.Range(1, 100).Select(i => new Person("Name" + i, i)).ToArray();

            _source.AddRange(people);
            _results.MessageCount().Should().Be(2);
            _results.NumberOfAdds().Should().Be(80);

            _results.Items().OrderBy(p => p.Age).ShouldAllBeEquivalentTo(people.Where(p => p.Age > 20).OrderBy(p => p.Age));
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
            _results.DataCount().Should().Be(0, "Should be nothing cached");
        }

        [Fact]
        public void BatchSuccessiveUpdates()
        {
            var people = Enumerable.Range(1, 100).Select(l => new Person("Name" + l, l)).ToArray();
            foreach (var person in people)
            {
                Person person1 = person;
                _source.Add(person1);
            }

            _results.MessageCount().Should().Be(81);
            _results.NumberOfAdds().Should().Be(80);
            _results.DataCount().Should().Be(80);

            _results.Data.Items.OrderBy(p => p.Age).ShouldAllBeEquivalentTo(_results.Data.Items.OrderBy(p => p.Age), "Incorrect Filter result");
        }

        [Fact]
        public void Clear()
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
        public void UpdateMatched()
        {
            const string key = "Adult1";
            var newperson = new Person(key, 50);
            var updated = new Person(key, 51);

            _source.Add(newperson);
            _source.Replace(newperson, updated);

            _results.MessageCount().Should().Be(3);
            _results.NumberOfAdds().Should().Be(1);
            _results.NumberOfReplaced().Should().Be(1);
            _results.DataCount().Should().Be(1);
        }

        [Fact]
        public void SameKeyChanges()
        {
            const string key = "Adult1";

            _source.Edit(updater =>
            {
                updater.Add(new Person(key, 50));
                updater.Add(new Person(key, 52));
                updater.Add(new Person(key, 53));
            });

            _results.MessageCount().Should().Be(2);
            _results.NumberOfAdds().Should().Be(3);
            _results.DataCount().Should().Be(3);
        }

        [Fact]
        public void UpdateNotMatched()
        {
            const string key = "Adult1";
            var newperson = new Person(key, 10);
            var updated = new Person(key, 11);

            _source.Add(newperson);
            _source.Replace(newperson, updated);

            _results.MessageCount().Should().Be(1);
            _results.NumberOfAdds().Should().Be(0);
            _results.DataCount().Should().Be(0);
        }

        #endregion
    }
}
