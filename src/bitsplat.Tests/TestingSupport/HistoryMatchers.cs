using NExpect.Implementations;
using NExpect.Interfaces;
using NExpect.MatcherLogic;

namespace bitsplat.Tests.TestingSupport
{
    public static class HistoryMatchers
    {
        public static void Match(
            this ITo<bitsplat.History.HistoryItem> to,
            bitsplat.History.HistoryItem expected)
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