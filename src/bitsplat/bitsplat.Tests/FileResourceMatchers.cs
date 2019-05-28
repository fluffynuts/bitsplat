using System.IO;
using bitsplat.Storage;
using NExpect.Implementations;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using PeanutButter.Utils;

namespace bitsplat.Tests
{
    public static class FileResourceMatchers
    {
        public static void Data(
            this IHave<IFileResource> have,
            byte[] expected)
        {
            have.AddMatcher(actual =>
            {
                if (!File.Exists(actual.Path))
                {
                    return new MatcherResult(
                        false,
                        () => $"Expected {false.AsNot()}to find file at: {actual.Path}");
                }

                using (var stream = actual.Read())
                {
                    var data = stream.ReadAllBytes();
                    var passed = expected.Length == data.Length &&
                                 data.DeepEquals(expected);
                    return new MatcherResult(
                        passed,
                        () =>
                        {
                            var actualHash = data.ToMD5String();
                            var expectedHash = expected.ToMD5String();
                            return $@"Expected {
                                    passed.AsNot()
                                } to find data with hash/length {
                                    expectedHash
                                }{
                                    expected.Length
                                }, but got {
                                    actualHash
                                }/{
                                    data.Length
                                }";
                        });
                }
            });
        }
    }
}