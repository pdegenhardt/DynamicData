﻿using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;

using DynamicData.Binding;
using DynamicData.Tests.Domain;

using FluentAssertions;

using Xunit;

namespace DynamicData.Tests.Binding;

public class ObservableCollectionBindCacheFixture : IDisposable
{
    private readonly IDisposable _binder;

    private readonly ObservableCollectionExtended<Person> _collection = new();

    private readonly RandomPersonGenerator _generator = new();

    private readonly ISourceCache<Person, string> _source;

    public ObservableCollectionBindCacheFixture()
    {
        _source = new SourceCache<Person, string>(p => p.Name);
        _binder = _source.Connect().Bind(_collection).Subscribe();
    }

    [Fact]
    public void AddToSourceAddsToDestination()
    {
        var person = new Person("Adult1", 50);
        _source.AddOrUpdate(person);

        _collection.Count.Should().Be(1, "Should be 1 item in the collection");
        _collection.First().Should().Be(person, "Should be same person");
    }

    [Fact]
    public void BatchAdd()
    {
        var people = _generator.Take(100).ToList();
        _source.AddOrUpdate(people);

        _collection.Count.Should().Be(100, "Should be 100 items in the collection");
        _collection.Should().BeEquivalentTo(_collection, "Collections should be equivalent");
    }

    [Fact]
    public void BatchRemove()
    {
        var people = _generator.Take(100).ToList();
        _source.AddOrUpdate(people);
        _source.Clear();
        _collection.Count.Should().Be(0, "Should be 100 items in the collection");
    }

    public void Dispose()
    {
        _binder.Dispose();
        _source.Dispose();
    }

    [Fact]
    public void RemoveSourceRemovesFromTheDestination()
    {
        var person = new Person("Adult1", 50);
        _source.AddOrUpdate(person);
        _source.Remove(person);

        _collection.Count.Should().Be(0, "Should be 1 item in the collection");
    }

    [Fact]
    public void UpdateToSourceSendsReplaceOnDestination()
    {
        var person = new Person("Adult1", 50);
        var anotherPerson = new Person("Adult1", 51);
        NotifyCollectionChangedAction action = default;
        _source.AddOrUpdate(person);

        using (_collection.ObserveCollectionChanges().Select(x => x.EventArgs.Action).Subscribe(updateType => action = updateType))
        {
            _source.AddOrUpdate(anotherPerson);
        }

        action.Should().Be(NotifyCollectionChangedAction.Replace, "The notification type should be Replace");
    }

    [Fact]
    public void UpdateToSourceUpdatesTheDestination()
    {
        var person = new Person("Adult1", 50);
        var personUpdated = new Person("Adult1", 51);
        _source.AddOrUpdate(person);
        _source.AddOrUpdate(personUpdated);

        _collection.Count.Should().Be(1, "Should be 1 item in the collection");
        _collection.First().Should().Be(personUpdated, "Should be updated person");
    }
}
