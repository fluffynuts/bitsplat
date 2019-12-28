using System;
using System.Linq;
using System.IO;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.Pipes;
using DryIoc;
using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    [TestFixture]
    public class DiscoveryTests
    {
        [TestFixture]
        public class MemoryStreams : DiscoveryTests
        {
            [Test]
            [Explicit("Discovery")]
            public void WriteMemStreamWithInitialData()
            {
                // Arrange
                var memStream = CreateMemoryStreamContaining(new byte[0]);
                var expected = GetRandomBytes(10);
                // Act
                memStream.Write(expected, 0, expected.Length);
                // Assert
                Expect(memStream.ToArray())
                    .To.Equal(expected);
            }

            private static MemoryStream CreateMemoryStreamContaining(
                byte[] data)
            {
                var result = new MemoryStream();
                result.Write(data, 0, data.Length);
                return result;
            }
        }

        [TestFixture]
        public class DryIocDiscovery : DiscoveryTests
        {
            [Test]
            [Explicit("Discovery")]
            public void ResolveMany()
            {
                // Arrange
                var container = new Container(r => r.WithTrackingDisposableTransients());
                var bitsplatAsm = typeof(Program).Assembly;
                var ignore = new[]
                {
                    typeof(ISink),
                    typeof(ISource),
                    typeof(IPipeElement),
                    typeof(IHistoryItem)
                };
                var bitSplatNamespace = typeof(Program).Namespace;
                var bitSplatNamespacePre = $"{bitSplatNamespace}.";
                container.RegisterMany(
                    new[] { bitsplatAsm },
                    type =>
                    {
                        if (!IsInBitSplatNamespace() ||
                            IsManuallyCreated())
                        {
                            return false;
                        }

                        return type.IsInterface && !type.IsGenericType;

                        bool IsInBitSplatNamespace()
                        {
                            return type.Namespace != null &&
                                   (
                                       type.Namespace == bitSplatNamespace ||
                                       type.Namespace.StartsWith(bitSplatNamespacePre)
                                   );
                        }

                        bool IsManuallyCreated()
                        {
                            return ignore.Contains(type);
                        }
                    }
                );
                // Act
                var allFilters = container.ResolveMany<IFilter>();
                // Assert
                Expect(allFilters)
                    .To.Contain.Exactly(1)
                    .Matched.By(o => o.GetType() == typeof(NoDotFilesFilter));
                Expect(allFilters)
                    .To.Contain.Exactly(1)
                    .Matched.By(o => o.GetType() == typeof(TargetOptInFilter));
                Expect(allFilters)
                    .To.Contain.Exactly(1)
                    .Matched.By(o => o.GetType() == typeof(SimpleTargetExistsFilter));
            }
        }
    }
}