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
                    kvp.Value
                )
            );

            result.Flags.ForEach(
                kvp => MapFlag(result, kvp.Key, kvp.Value)
            );
        }

        private void MapFlag<T>(
            T result,
            string key,
            ParsedArgument<bool> arg)
        {
            if (!PropertiesOf(typeof(T))
                    .TryGetValue(key, out var propertyInfo))
            {
                return;
            }

            if (propertyInfo.PropertyType != typeof(bool))
            {
                throw new InvalidOperationException(
                    $"Cannot map flag {key} onto property of type {propertyInfo.PropertyType}"
                );
            }
            propertyInfo.SetValue(result, arg.Value);
        }

        private void MapParameter<T>(
            T result,
            string key,
            ParsedArgument<string[]> arg
        ) where T : ParsedArguments
        {
            if (!PropertiesOf(typeof(T))
                    .TryGetValue(key, out var propInfo))
            {
                return;
            }

            var wasMapped = Mappers.Aggregate(
                false,
                (acc, cur) => acc || cur(propInfo, arg, result)
            );

            if (wasMapped)
            {
                return;
            }

            if (arg.IsRequired)
            {
                throw new ArgumentException(
                    $"Argument: {key} is required"
                );
            }
        }

        private static Func<
                PropertyInfo,
                ParsedArgument<string[]>,
                ParsedArguments,
                bool>[]
            Mappers =
            {
                MapEnum,
                MapSingle,
                MapMulti
            };

        private static bool MapMulti(
            PropertyInfo pi,
            ParsedArgument<string[]> parsed,
            ParsedArguments result)
        {
            var underlyingType = pi.PropertyType.GetCollectionItemType();
            if (underlyingType == null)
            {
                return false;
            }

            var converted = parsed.Value.Select(
                    stringValue =>
                    {
                        var couldConvert = TryChangeType(stringValue, underlyingType, out var convertedValue);
                        return (couldConvert, convertedValue);
                    })
                .Where(o => o.couldConvert)
                .Select(o => o.convertedValue)
                .ToArray();
            if (converted.Length != parsed.Value.Length)
            {
                // TODO: throw: one or more items could not be converted
            }

            var propertyValue = converted.CastCollection(
                underlyingType,
                pi.PropertyType.IsArray);

            // TODO: handle other collection types
            pi.SetValue(result, propertyValue);
            return true;
        }

        private static bool MapSingle(
            PropertyInfo pi,
            ParsedArgument<string[]> parsed,
            ParsedArguments result)
        {
            return MapSingle(
                pi,
                parsed,
                result,
                () => !pi.PropertyType.IsCollection(),
                (desiredType, value) => TryChangeType(value, desiredType, out var converted)
                                            ? (true, converted)
                                            : (false, converted)
            );
        }

        private static bool MapEnum(
            PropertyInfo pi,
            ParsedArgument<string[]> parsed,
            ParsedArguments result)
        {
            return MapSingle(
                pi,
                parsed,
                result,
                () => pi.PropertyType.IsEnum,
                (type, value) => Enum.TryParse(type, value, true, out var converted)
                                     ? (true, value: converted)
                                     : (false, value: converted)
            );
        }

        private static bool MapSingle(
            PropertyInfo pi,
            ParsedArgument<string[]> parsed,
            ParsedArguments result,
            Func<bool> propertyTypeCheck,
            Func<Type, string, (bool success, object converted)> parser)
        {
            if (!propertyTypeCheck())
            {
                return false;
            }

            if (parsed.Value.Length == 0)
            {
                return true;
            }

            if (parsed.Value.Length > 1)
            {
                throw new ArgumentException(
                    $"{pi.Name} expects exactly ONE value"
                );
            }

            var parseResult = parser.Invoke(
                pi.PropertyType,
                parsed.Value[0]);
            if (!parseResult.success)
            {
                throw new ArgumentException(
                    $"Unable to parse '{parsed.Value[0]}' as a value for {pi.Name}"
                );
            }

            pi.SetValue(result, parseResult.converted);
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
                Value = parser.Parse(args),
                IsRequired = parser.IsRequired
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
                Value = parser.Parse(args),
                IsRequired = parser.IsRequired
            };
        }
    }
}