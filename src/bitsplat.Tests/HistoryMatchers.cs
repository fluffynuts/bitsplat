using NExpect.Implementations;
using NExpect.Interfaces;
using NExpect.MatcherLogic;

namespace bitsplat.Tests
{
    public static class HistoryMatchers
    {
        public static void Match(
            this ITo<History.History> to,
            History.History expected)
        {
            to.AddMatcher(actual =>
            {
                var passed = actual.Path == expected.Path &&
                             actual.Size == expected.Size &&
                             actual.Created >= expected.Created;
                return new MatcherResult(
                    passed,
                    () => $"Expected {actual.Stringify()} {passed.AsNot()}to match {expected.Stringify()}"
                );
            });
        }
    }
}