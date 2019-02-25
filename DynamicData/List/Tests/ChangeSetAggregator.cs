using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Diagnostics;

// ReSharper disable once CheckNamespace
namespace DynamicData.Tests
{
    internal static class ListChangeSetAggregatorEx
    {

        public static int TotalChangeCount<TObject>(this ChangeSetAggregator<TObject> self)
        {
            return self.ChangeSum(changes => changes.TotalChanges);
        }

        public static int NumberOfAdds<TObject>(this ChangeSetAggregator<TObject> self)
        {
            return self.ChangeSum(changes => changes.Adds);
        }

        public static int NumberOfRemoves<TObject>(this ChangeSetAggregator<TObject> self)
        {
            return self.ChangeSum(changes => changes.Removes);
        }

        public static int NumberOfRefreshes<TObject>(this ChangeSetAggregator<TObject> self)
        {
            return self.ChangeSum(changes => changes.Refreshes);
        }

        public static int NumberOfReplaced<TObject>(this ChangeSetAggregator<TObject> self)
        {
            return self.ChangeSum(changes => changes.Replaced);
        }

        public static int ChangeSum<TObject>(this ChangeSetAggregator<TObject> self, Func<IChangeSet<TObject>, int> totalSelector)
        {
            return self.Messages.Select(totalSelector).Sum();
        }

        public static int DataCount<TObject>(this ChangeSetAggregator<TObject> self)
        {
            return self.Data.Count;
        }

        public static IEnumerable<TObject> Items<TObject>(this ChangeSetAggregator<TObject> self)
        {
            return self.Data.Items;
        }

        public static int MessageCount<TObject>(this ChangeSetAggregator<TObject> self)
        {
            return self.Messages.Count;
        }

        public static IChangeSet<TObject> LastMessage<TObject>(this ChangeSetAggregator<TObject> self)
        {
            return self.Messages.Last();
        }

    }

    /// <summary>
    /// Aggregates all events and statistics for a changeset to help assertions when testing
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    public class ChangeSetAggregator<TObject> : IDisposable
    {
        private readonly IDisposable _disposer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetAggregator{TObject, TKey}"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        public ChangeSetAggregator(IObservable<IChangeSet<TObject>> source)
        {
            var published = source.Publish();

            Data = published.AsObservableList();

            var results = published.Subscribe(updates => Messages.Add(updates));
            var connected = published.Connect();
            
            _disposer = Disposable.Create(() =>
            {
                Data.Dispose();
                connected.Dispose();
                results.Dispose();
            });
        }

        /// <summary>
        /// A clone of the data
        /// </summary>
        public IObservableList<TObject> Data { get; }

        /// <summary>
        /// All message received
        /// </summary>
        public IList<IChangeSet<TObject>> Messages { get; } = new List<IChangeSet<TObject>>();


        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            _disposer.Dispose();
        }
    }
}
