using System;
using System.Collections.Generic;

namespace bitsplat.CommandLine
{
    public static class CollectionExtensions
    {
        public static object CastCollection<T>(
            this IEnumerable<T> values,
            Type desiredType,
            bool asArray)
        {
            var listType = GenericListType.MakeGenericType(desiredType);
            var asList = Activator.CreateInstance(listType);
            var addMethod = listType.GetMethod("Add");
            if (addMethod == null)
            {
                throw new InvalidOperationException(
                    $"{listType} has no 'Add' method?!"
                );
            }

            values.ForEach(v =>
            {
                addMethod.Invoke(
                    asList,
                    new object[] { v }
                );
            });
            return asArray
                       ? ConvertToArray(asList)
                       : asList;
        }

        private static object ConvertToArray(
            object list)
        {
            var toArray = list.GetType()
                .GetMethod("ToArray");
            if (toArray == null)
            {
                throw new InvalidOperationException(
                    $"{list.GetType()} has no ToArray method"
                );
            }

            return toArray.Invoke(list, new object[0]);
        }

        private static readonly Type GenericListType
            = typeof(List<>);
    }
}