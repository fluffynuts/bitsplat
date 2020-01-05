using System;
using System.Collections.Generic;
using System.Linq;

namespace bitsplat
{
    public static class BrailleProgressBarGenerator
    {
        public static string CompositeProgressBarFor(
            int itemPercentage,
            int totalPercentage)
        {
            return string.Join(
                "",
                BarItemFor(0, 20, itemPercentage, totalPercentage),
                BarItemFor(20, 40, itemPercentage, totalPercentage),
                BarItemFor(40, 60, itemPercentage, totalPercentage),
                BarItemFor(60, 80, itemPercentage, totalPercentage),
                BarItemFor(80, 100, itemPercentage, totalPercentage)
            );
        }

        private static string BarItemFor(
            int min,
            int max,
            int top,
            int bottom)
        {
            var range = max - min;
            var topPerc = Percentage(top - min, range);
            var bottomPerc = Percentage(bottom - min, range);
            var topPartial = ResolvePartial(topPerc, _topPartials);
            var bottomPartial = ResolvePartial(bottomPerc, _bottomPartials);
            return _compositeResolutions[$"{topPartial}{bottomPartial}"];
        }

        private static string ResolvePartial(
            int value,
            (int min, int max, string resolution)[] resolutions)
        {
            if (value > 100)
            {
                return resolutions.Last()
                    .resolution;
            }

            return resolutions.Aggregate(
                       null as string,
                       (acc, cur) => value > cur.min && value <= cur.max
                                         ? cur.resolution
                                         : acc
                   ) ??
                   " ";
        }

        private static int Percentage(double value, double total)
        {
            return (int) (Math.Round(100 * value / total));
        }

        private static readonly (int min, int max, string resolution)[] _topPartials =
        {
            (0, 20, " "),
            (20, 40, "⠁"),
            (40, 60, "⠃"),
            (60, 80, "⠋"),
            (80, 100, "⠛")
        };

        private static readonly (int min, int max, string resolution)[] _bottomPartials =
        {
            (0, 20, " "),
            (20, 40, "⡀"),
            (40, 60, "⡄"),
            (60, 80, "⣄"),
            (80, 100, "⣤")
        };

        private static Dictionary<string, string> _compositeResolutions = new Dictionary<string, string>()
        {
            // {top}{bottom} => {composite}
            ["  "] = " ",
            ["⠁ "] = "⠁",
            ["⠃ "] = "⠃",
            ["⠋ "] = "⠋",
            ["⠛ "] = "⠛",

            [" ⡀"] = "⡀",
            ["⠁⡀"] = "⡁",
            ["⠃⡀"] = "⡅",
            ["⠋⡀"] = "⡋",
            ["⠛⡀"] = "⡛",

            [" ⡄"] = "⡄",
            ["⠁⡄"] = "⡅",
            ["⠃⡄"] = "⡇",
            ["⠋⡄"] = "⡏",
            ["⠛⡄"] = "⡟",

            [" ⣄"] = "⣄",
            ["⠁⣄"] = "⣅",
            ["⠃⣄"] = "⣇",
            ["⠋⣄"] = "⣏",
            ["⠛⣄"] = "⣟",

            [" ⣤"] = "⣤",
            ["⠁⣤"] = "⣥",
            ["⠃⣤"] = "⣧",
            ["⠋⣤"] = "⣯",
            ["⠛⣤"] = "⣿",
        };
    }
}