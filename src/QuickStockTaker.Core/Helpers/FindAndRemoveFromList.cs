using System;
using System.Collections.Generic;
using System.Text;

namespace QuickStockTaker.Core.Helpers
{
    /// <summary>
    /// Find and remove an item from a collection
    /// </summary>
    public static class FindAndRemoveFromList
    {
        public static List<T> FindAndRemove<T>(this List<T> lst, Predicate<T> match)
        {
            List<T> ret = lst.FindAll(match);
            lst.RemoveAll(match);
            return ret;
        }
    }
}
