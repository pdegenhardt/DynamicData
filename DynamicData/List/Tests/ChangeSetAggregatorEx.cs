using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData.Tests;

namespace DynamicData.List.Tests
{
    internal static class ChangeSetAggregatorEx
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
}