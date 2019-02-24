using System;
using System.Linq;
using DynamicData.Tests.Domain;
using Microsoft.Reactive.Testing;
using Xunit;
using FluentAssertions;

namespace DynamicData.Tests.List
{
    
    public class SizeLimitFixture: IDisposable
    {
        private readonly ISourceList<Person> _source;
        private readonly ChangeSetAggregator<Person> _results;
        private readonly TestScheduler _scheduler;
        private readonly IDisposable _sizeLimiter;
        private readonly RandomPersonGenerator _generator = new RandomPersonGenerator();


        public  SizeLimitFixture()
        {
            _scheduler = new TestScheduler();
            _source = new SourceList<Person>();
            _sizeLimiter = _source.LimitSizeTo(10, _scheduler).Subscribe();
            _results = _source.Connect().AsAggregator();
        }

        public void Dispose()
        {
            _sizeLimiter.Dispose();
            _source.Dispose();
            _results.Dispose();
        }

        [Fact]
        public void AddLessThanLimit()
        {
            var person = _generator.Take(1).First();
            _source.Add(person);

            _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(150).Ticks);

            _results.MessageCount().Should().Be(2);
            _results.DataCount().Should().Be(1);
            _results.Items().First().Should().Be(person);
        }

        [Fact]
        public void AddMoreThanLimit()
        {
            var people = _generator.Take(100).OrderBy(p => p.Name).ToArray();
            _source.AddRange(people);
            _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);

            _source.Dispose();
            _results.DataCount().Should().Be(10);
            
            _results.MessageCount().Should().Be(3);
            _results.NumberOfAdds().Should().Be(100);
            _results.NumberOfRemoves().Should().Be(90);
        }

        [Fact]
        public void AddMoreThanLimitInBatched()
        {
            _source.AddRange(_generator.Take(10).ToArray());
            _source.AddRange(_generator.Take(10).ToArray());

            _scheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);

            _results.DataCount().Should().Be(10);
            _results.MessageCount().Should().Be(4);
            _results.NumberOfAdds().Should().Be(20);
            _results.NumberOfRemoves().Should().Be(10);
        }

        [Fact]
        public void Add()
        {
            var person = _generator.Take(1).First();
            _source.Add(person);

            _results.DataCount().Should().Be(1);
            _results.MessageCount().Should().Be(2);
        }

        [Fact]

        public void ForceError()
        {
            var person = _generator.Take(1).First();
            Assert.Throws<ArgumentOutOfRangeException>(() => _source.RemoveAt(1));
        }

        [Fact]
        public void ThrowsIfSizeLimitIsZero()
        {
            // Initialise();
            Assert.Throws<ArgumentException>(() => new SourceCache<Person, string>(p => p.Key).LimitSizeTo(0));
        }
    }
}
