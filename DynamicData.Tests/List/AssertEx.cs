using System;
using System.Collections.Generic;
using System.Text;

namespace DynamicData.Tests.List
{
    public static class AssertEx
    {

        public static int NumberOfAdds<TObject>(this ChangeSetAggregator<TObject> self)
        {
            return self.ChangeSum(changes => changes.Adds);
        }
    }
}
