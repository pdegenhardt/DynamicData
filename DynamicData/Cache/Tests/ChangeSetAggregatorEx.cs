using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData.Tests;

namespace DynamicData.Cache.Tests
{
    internal static class ChangeSetAggregatorEx
    {
        public static int TotalChangeCount<TObject, TKey>(this ChangeSetAggregator<TObject, TKey> self)
        {
            return self.ChangeSum(changes => changes.Count);
        }

        public static int NumberOfAdds<TObject, TKey>(this ChangeSetAggregator<TObject, TKey> self)
        {
            return self.ChangeSum(changes => changes.Adds);
        }

        public static int NumberOfRemoves<TObject, TKey>(this ChangeSetAggregator<TObject, TKey> self)
        {
            return self.ChangeSum(changes => changes.Removes);
        }

        public static int NumberOfRefreshes<TObject, TKey>(this ChangeSetAggregator<TObject, TKey> self)
        {
            return self.ChangeSum(changes => changes.Refreshes);
        }

        public static int NumberOfUpdates<TObject, TKey>(this ChangeSetAggregator<TObject, TKey> self)
        {
            return self.ChangeSum(changes => changes.Updates);
        }

        public static int ChangeSum<TObject, TKey>(this ChangeSetAggregator<TObject, TKey> self, Func<IChangeSet<TObject, TKey>, int> totalSelector)
        {
            return self.Messages.Select(totalSelector).Sum();
        }

        public static int DataCount<TObject, TKey>(this ChangeSetAggregator<TObject, TKey> self)
        {
            return self.Data.Count;
        }

        public static IEnumerable<TObject> Items<TObject, TKey>(this ChangeSetAggregator<TObject, TKey> self)
        {
            return self.Data.Items;
        }

        public static int MessageCount<TObject, TKey>(this ChangeSetAggregator<TObject, TKey> self)
        {
            return self.Messages.Count;
        }

        public static IChangeSet<TObject, TKey> LastMessage<TObject, TKey>(this ChangeSetAggregator<TObject, TKey> self)
        {
            return self.Messages.Last();
        }

    }
}