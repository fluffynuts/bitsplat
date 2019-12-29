using System.Collections.Generic;
using System.Linq;

namespace bitsplat.CommandLine
{
    public static class Args
    {
        public static bool FindFlag(
            this IList<string> args,
            params string[] switches)
        {
            return args.TryFindFlag(switches) ?? false;
        }

        public static bool? TryFindFlag(
            this IList<string> args,
            params string[] switches)
        {
            return switches.Aggregate(
                null as bool?,
                (acc, cur) =>
                {
                    int idx;
                    while ((idx = args.IndexOf(cur)) > -1)
                    {
                        args.RemoveAt(idx);
                        acc = true;
                    }

                    return acc;
                });
        }

        public static string[] FindParameters(
            this IList<string> args,
            params string[] switches)
        {
            var result = new List<string>();
            var toRemove = new List<int>();
            var inSwitch = false;
            args.ForEach((arg, idx) =>
            {
                if (switches.Contains(arg))
                {
                    inSwitch = true;
                    toRemove.Add(idx);
                    return;
                }

                if (!inSwitch)
                {
                    return;
                }

                inSwitch = false;
                result.Add(arg);
                toRemove.Add(idx);
            });

            toRemove.ForEach((removeIndex, idx) =>
            {
                args.RemoveAt(removeIndex - idx);
            });

            return result.ToArray();
        }

        public static ArgumentsBuilder Configure()
        {
            return new ArgumentsBuilder();
        }
    }
}