using System.Linq;
using NExpect.Implementations;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using PeanutButter.Utils;

namespace bitsplat.Tests.TestingSupport
{
    public static class Matchers
    {
        /// <summary>
        /// Asserts that the path is a directory
        /// </summary>
        /// <param name="a"></param>
        public static void Directory(this IA<string> a)
        {
            a.AddMatcher(actual =>
            {
                var passed = System.IO.Directory.Exists(actual);
                return new MatcherResult(passed, () => $"Expected {actual} {passed.AsNot()}to exist");
            });
        }

        /// <summary>
        /// Asserts that the path has some contents (file(s) and/or folder(s))
        /// </summary>
        /// <param name="have"></param>
        public static void Contents(this IHave<string> have)
        {
            have.AddMatcher(TestContents);
        }

        private static MatcherResult TestContents(
            string path)
        {
                var hasFiles = System.IO.Directory.GetFiles(path)
                    .Any();
                var hasDirectories = System.IO.Directory.GetDirectories(path)
                    .Any();
                var passed = hasFiles || hasDirectories;
                return new MatcherResult(
                    passed,
                    () => $"Expected {path} {passed.AsNot()}to have contents"
                );
        }

        /// <summary>
        /// Asserts that the auto temp folder has some contents
        /// </summary>
        /// <param name="have"></param>
        public static void Contents(
            this IHave<AutoTempFolder> have)
        {
             have.AddMatcher(actual => TestContents(actual.Path));
        }
    }
}