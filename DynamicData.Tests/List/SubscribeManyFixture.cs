using System;
using System.Linq;
using System.Reactive.Disposables;
using DynamicData.List.Tests;
using FluentAssertions;
using Xunit;

namespace DynamicData.Tests.List
{
    
    public class SubscribeManyFixture: IDisposable
    {
        private class SubscribeableObject
        {
            public bool IsSubscribed { get; private set; }
            private int Id { get; }

            public void Subscribe()
            {
                IsSubscribed = true;
            }

            public void UnSubscribe()
            {
                IsSubscribed = false;
            }

            public SubscribeableObject(int id)
            {
                Id = id;
            }
        }

        private readonly ISourceList<SubscribeableObject> _source;
        private readonly ChangeSetAggregator<SubscribeableObject> _results;

        public  SubscribeManyFixture()
        {
            _source = new SourceList<SubscribeableObject>();
            _results = new ChangeSetAggregator<SubscribeableObject>(
                _source.Connect().SubscribeMany(subscribeable =>
                {
                    subscribeable.Subscribe();
                    return Disposable.Create(subscribeable.UnSubscribe);
                }));
        }

        public void Dispose()
        {
            _source.Dispose();
            _results.Dispose();
        }

        [Fact]
        public void AddedItemWillbeSubscribed()
        {
            _source.Add(new SubscribeableObject(1));

            _results.MessageCount().Should().Be(2);
            _results.DataCount().Should().Be(1);
            _results.Items().First().IsSubscribed.Should().Be(true, "Should be subscribed");
        }

        [Fact]
        public void RemoveIsUnsubscribed()
        {
            _source.Add(new SubscribeableObject(1));
            _source.RemoveAt(0);

            _results.MessageCount().Should().Be(3);
            _results.DataCount().Should().Be(0);
            _results.LastMessage().First().Item.Current.IsSubscribed.Should().Be(false, "Should be be unsubscribed");
        }

        //[Fact]
        //public void UpdateUnsubscribesPrevious()
        //{
        //	_source.Add(new SubscribeableObject(1));
        //	_source.AddOrUpdate(new SubscribeableObject(1)));

        //	Assert.AreEqual(2, _results.MessageCount(), "Should be 2 updates");
        //	Assert.AreEqual(1, _results.DataCount(), "Should be 1 items in the cache");
        //	Assert.AreEqual(true, _results.Messages[1].First().Current.IsSubscribed, "Current should be subscribed");
        //	Assert.AreEqual(false, _results.Messages[1].First().Previous.Value.IsSubscribed, "Previous should not be subscribed");
        //}

        [Fact]
        public void EverythingIsUnsubscribedWhenStreamIsDisposed()
        {
            _source.AddRange(Enumerable.Range(1, 10).Select(i => new SubscribeableObject(i)));
            _source.Clear();

            var items = _results.Messages[0].SelectMany(x => x.Range);
            items.All(d => !d.IsSubscribed).Should().BeTrue();
        }
    }
}
