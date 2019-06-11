using System;
using System.Collections.Generic;

namespace bitsplat
{
    // copied from PeanutButter.Utils, to try to keep
    //  dependencies to a minimum
    internal static class EnumerableExtensions
    {
        internal static void ForEach<T>(
            this IEnumerable<T> collection,
            Action<T> action)
        {
            if (collection == null)
            {
                return;
            }

            foreach (var item in collection)
            {
                action?.Invoke(item);
            }
        }
    }
}