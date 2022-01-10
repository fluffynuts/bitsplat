using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace bitsplat.CommandLine
{
    public static class StringExtensions
    {
        public static string[] SplitLines(this string str, int maxLength)
        {
            var start = 0;
            var result = new List<string>();
            while (start < str.Length)
            {
                var end = maxLength;
                if (start + end > str.Length)
                {
                    end = str.Length - start;
                }

                result.Add(str.Substring(start, end));
                start += end;
            }
            return result.ToArray();
        }

        public static string[] SplitPath(this string str)
        {
            return Regex.Split(
                str ?? "",
                "[/||\\\\]"
            );
        }

        public static bool IsDotFile(this string path)
        {
            return path?.SplitPath()?.Last()?.IndexOf('.') == 0;
        }
    }
}