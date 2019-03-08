using System;
using System.Linq;
using System.Reactive.Subjects;
using DynamicData.Aggregation;
using DynamicData.Tests.Domain;

using Xunit;
using System.Collections.Generic;
using FluentAssertions;
using DynamicData.List.Tests;


namespace DynamicData.Tests.List
{
    
    public class FilterWithObservable: IDisposable
    {
        private readonly ISourceList<Person> _source;
        private readonly ChangeSetAggregator<Person> _results;
        private readonly BehaviorSubject<Func<Person, bool>> _filter;

        public  FilterWithObservable()
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
            _results.DataCount().Should().Be(50, "Should be 50 people in the cache");
            _results.MessageCount().Should().Be(3);

            _results.Items().All(p => p.Age <= 50).Should().BeTrue();
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
            _filter.OnNext(_filter.Value);

            _results.DataCount().Should().Be(90);
            _results.MessageCount().Should().Be(3);
            _results.NumberOfAdds().Should().Be(90);

            foreach (var person in people)
            {
                person.Age = person.Age - 10;
            }
            _filter.OnNext(_filter.Value);

            _results.DataCount().Should().Be(80);
        }

        [Fact]
        public void RemoveFiltered()
        {
            var person = new Person("P1", 1);

            _source.Add(person);
            _results.Data.Count.Should().Be(0, "Should be 0 people in the cache");
            _filter.OnNext(p => p.Age >= 1);
            _results.Data.Count.Should().Be(1, "Should be 1 people in the cache");

            _source.Remove(person);

            _results.Data.Count.Should().Be(0, "Should be 0 people in the cache");
        }

        [Fact]
        public void RemoveFilteredRange()
        {
            var people = Enumerable.Range(1, 10).Select(i => new Person("P" + i, i)).ToArray();

            _source.AddRange(people);
            _results.Data.Count.Should().Be(0, "Should be 0 people in the cache");
            _filter.OnNext(p => p.Age > 5);
            _results.Data.Count.Should().Be(5, "Should be 5 people in the cache");

            _source.RemoveRange(5, 5);

            _results.Data.Count.Should().Be(0, "Should be 0 people in the cache");
        }

        [Fact]
        public void ChainFilters()
        {
            var filter2 = new BehaviorSubject<Func<Person, bool>>(person1 => person1.Age > 20);

            var stream = _source.Connect()
                                .Filter(_filter)
                                .Filter(filter2);

            var captureList = new List<int>();
            stream.Count().Subscribe(count => captureList.Add(count));

            var person = new Person("P", 30);
            _source.Add(person);

            person.Age = 10;
            _filter.OnNext(_filter.Value);

            captureList.Should().BeEquivalentTo(new[] {0, 1, 0});
        }

        #region Static filter tests

        /* Should be the same as standard lambda filter */

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
        public void AddNotMatched()
        {
            var person = new Person("Adult1", 10);
            _source.Add(person);

            _results.MessageCount().Should().Be(1);
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
            _results.Messages[1].First().Range.First().Should().Be(matched);
            _results.Data.Items.First().Should().Be(matched);
        }

        [Fact]
        public void AttemptedRemovalOfANonExistentKeyWillBeIgnored()
        {
            _source.Remove(new Person("anyone", 1));
            _results.NumberOfRemoves().Should().Be(0);
            _results.MessageCount().Should().Be(1);
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
            _results.DataCount().Should().Be(0);
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
            _results.DataCount().Should().Be(80);
            
            _results.Items().OrderBy(p => p.Age).ShouldAllBeEquivalentTo(people.Where(p => p.Age > 20).OrderBy(p => p.Age));
        }

        [Fact]
        public void Clear()
        {
            var people = Enumerable.Range(1, 100).Select(l => new Person("Name" + l, l)).ToArray();
            _source.AddRange(people);
            _source.Clear();

            _results.MessageCount().Should().Be(3);
            _results.Messages[1].First().Reason.Should().Be(ListChangeReason.AddRange);
            _results.Messages[2].First().Reason.Should().Be(ListChangeReason.Clear);
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
                //    updater.Remove(key);
            });

            _results.MessageCount().Should().Be(2);
            _results.NumberOfAdds().Should().Be(3);
        }

        [Fact]
        public void UpdateNotMatched()
        {
            const string key = "Adult1";
            var notamatch = new Person(key, 10);
            var stillNotAMatch = new Person(key, 11);

            _source.Add(notamatch);
            _source.Replace(notamatch, stillNotAMatch);

            _results.MessageCount().Should().Be(1);
            _results.NumberOfAdds().Should().Be(0);
            _results.NumberOfRemoves().Should().Be(0);
            _results.DataCount().Should().Be(0);
        }

        #endregion
    }
}
