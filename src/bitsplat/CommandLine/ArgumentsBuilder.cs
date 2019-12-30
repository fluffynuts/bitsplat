using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PeanutButter.Utils;

namespace bitsplat.CommandLine
{
    public class ArgumentsBuilder
    {
        private readonly Dictionary<string, Action<FlagParser>> _flags
            = new Dictionary<string, Action<FlagParser>>();

        private readonly Dictionary<string, Action<ParameterParser>>
            _parameters = new Dictionary<string, Action<ParameterParser>>();

        public ArgumentsBuilder WithFlag(
            string name,
            Action<FlagParser> configuration)
        {
            _flags[name] = configuration;
            return this;
        }

        public ArgumentsBuilder WithParameter(
            string name,
            Action<ParameterParser> configuration)
        {
            _parameters[name] = configuration;
            return this;
        }

        public ParsedArguments Parse(
            string[] args
        )
        {
            return Parse<ParsedArguments>(args);
        }

        public T Parse<T>(
            string[] args
        ) where T : ParsedArguments, new()
        {
            var result = new T();
            result.RawArguments = args.ToArray(); // copy

            var argsList = args.ToList();
            _flags.ForEach(
                kvp => ParseFlag(
                    result,
                    argsList,
                    kvp.Key,
                    kvp.Value
                )
            );
            _parameters.ForEach(
                kvp => ParseParameter(
                    result,
                    argsList,
                    kvp.Key,
                    kvp.Value
                )
            );
            MapCustomProperties(result);
            return result;
        }

        private void MapCustomProperties<T>(T result)
            where T : ParsedArguments
        {
            if (typeof(T) == typeof(ParsedArguments))
            {
                // nothing to do here; bail out
                return;
            }

            result.Parameters.ForEach(
                kvp => MapParameter(
                    result,
                    kvp.Key,
                    kvp.Value.Value
                )
            );
        }

        private void MapParameter<T>(T result,
            string key,
            string[] values
        ) where T : ParsedArguments
        {
            if (!PropertiesOf(typeof(T))
                    .TryGetValue(key, out var propInfo))
            {
                return;
            }

            var wasMapped = Mappers.Aggregate(
                false,
                (acc, cur) => acc || cur(propInfo, values, result)
            );

            if (!wasMapped)
            {
                // TODO
            }
        }

        private static Func<PropertyInfo, string[], ParsedArguments, bool>[]
            Mappers =
            {
                MapEnum,
                MapSingle,
                MapMulti
            };

        private static bool MapMulti(
            PropertyInfo pi,
            string[] values,
            ParsedArguments parsedArguments)
        {
            var underlyingType = pi.PropertyType.GetCollectionItemType();
            if (underlyingType == null)
            {
                return false;
            }

            var converted = values.Select(
                    stringValue =>
                    {
                        var couldConvert = TryChangeType(stringValue, underlyingType, out var convertedValue);
                        return (couldConvert, convertedValue);
                    })
                .Where(o => o.couldConvert)
                .Select(o => o.convertedValue)
                .ToArray();
            if (converted.Length != values.Length)
            {
                // TODO: throw: one or more items could not be converted
            }

            var propertyValue = converted.CastCollection(
                underlyingType,
                pi.PropertyType.IsArray);

            // TODO: handle other collection types
            pi.SetValue(parsedArguments, propertyValue);
            return true;
        }

        private static bool MapSingle(
            PropertyInfo pi,
            string[] values,
            ParsedArguments parsedArguments)
        {
            return MapSingle(
                pi,
                values,
                parsedArguments,
                () => !pi.PropertyType.IsCollection(),
                (desiredType, value) => TryChangeType(value, desiredType, out var converted)
                                            ? (true, converted)
                                            : (false, converted)
            );
        }

        private static bool MapEnum(
            PropertyInfo pi,
            string[] values,
            ParsedArguments parsedArguments)
        {
            return MapSingle(
                pi,
                values,
                parsedArguments,
                () => pi.PropertyType.IsEnum,
                (type, value) => Enum.TryParse(type, value, out var converted)
                                     ? (true, value: converted)
                                     : (false, value: converted)
            );
        }

        private static bool MapSingle(
            PropertyInfo pi,
            string[] values,
            ParsedArguments parsedArguments,
            Func<bool> propertyTypeCheck,
            Func<Type, string, (bool success, object converted)> parser)
        {
            if (!propertyTypeCheck())
            {
                return false;
            }

            if (values.Length != 1)
            {
                throw new ArgumentException(
                    $"{pi.Name} expects exactly ONE value"
                );
            }

            var parseResult = parser.Invoke(
                pi.PropertyType,
                values[0]);
            if (!parseResult.success)
            {
                throw new ArgumentException(
                    $"Unable to parse '{values[0]}' as a value for {pi.Name}"
                );
            }

            pi.SetValue(parsedArguments, parseResult.converted);
            return true;
        }

        private static bool TryChangeType(
            string value,
            Type desiredType,
            out object converted)
        {
            if (desiredType == typeof(string))
            {
                converted = value;
                return true;
            }

            try
            {
                converted = Convert.ChangeType(value, desiredType);
                return true;
            }
            catch
            {
                converted = null;
                return false;
            }
        }

        private Dictionary<string, PropertyInfo> PropertiesOf(
            Type type)
        {
            if (_propertyCache.TryGetValue(type, out var result))
            {
                return result;
            }

            return _propertyCache[type] = ReadPropertiesOf(type);
        }

        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>>
            _propertyCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        private Dictionary<string, PropertyInfo> ReadPropertiesOf(Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(
                    pi => pi.Name,
                    pi => pi,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        private void ParseParameter(
            ParsedArguments result,
            IList<string> args,
            string name,
            Action<ParameterParser> configure)
        {
            var parser = new ParameterParser(name);
            configure(parser);
            result.Parameters[name] = new ParsedArgument<string[]>()
            {
                Value = parser.Parse(args)
            };
        }

        private void ParseFlag(
            ParsedArguments result,
            IList<string> args,
            string name,
            Action<FlagParser> configure)
        {
            var parser = new FlagParser(name);
            configure(parser);
            result.Flags[name] = new ParsedArgument<bool>()
            {
                Value = parser.Parse(args)
            };
        }
    }

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