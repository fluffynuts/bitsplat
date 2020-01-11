using System.IO;
using System.Linq;
using bitsplat.CommandLine;
using bitsplat.StaleFileRemovers;
using bitsplat.Storage;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using NExpect;
using NSubstitute;
using PeanutButter.RandomGenerators;
using PeanutButter.Utils;
using static NExpect.Expectations;
using static bitsplat.Tests.RandomValueGen;

namespace bitsplat.Tests.StaleFileRemovers
{
    public class TestDefaultStaleFileRemover
    {
        [Test]
        public void ShouldRemoveTargetFilesNotFoundAtSource()
        {
            // Arrange
            var commonResources = GetRandomArray<IReadWriteFileResource>(2);
            var onlyAtTarget = GetRandom<IReadWriteFileResource>();
            var source = Substitute.For<IFileSystem>();
            var target = Substitute.For<IFileSystem>();
            source.ListResourcesRecursive()
                .Returns(commonResources);
            target.ListResourcesRecursive()
                .Returns(commonResources.And(onlyAtTarget));
            var sut = Create();
            // Act
            sut.RemoveStaleFiles(
                source,
                target);
            // Assert
            Expect(source)
                .Not.To.Have.Received()
                .Delete(Arg.Any<string>());
            Expect(target)
                .To.Have.Received(1)
                .Delete(Arg.Any<string>());
            Expect(target)
                .To.Have.Received(1)
                .Delete(onlyAtTarget.RelativePath);
        }

        private static IStaleFileRemover Create()
        {
            return new DefaultStaleFileRemover();
        }
    }

    public class ReadWriteFileResourceBuilder
        : GenericBuilder<ReadWriteFileResourceBuilder, IReadWriteFileResource>
    {
        public override ReadWriteFileResourceBuilder WithRandomProps()
        {
            return WithRelativePath(GetRandomPath(2))
                .WithData(GetRandomBytes(1024, 4196));
        }

        public ReadWriteFileResourceBuilder WithData(
            byte[] data)
        {
            return WithProp(o =>
            {
                o.Size.Returns(data.Length);
                o.OpenForRead()
                    .Returns(new MemoryStream(data));
                // TODO: if OpenForWrite needs to work both ways, this should be updated
                o.OpenForWrite()
                    .Returns(new MemoryStream());
            });
        }

        public ReadWriteFileResourceBuilder WithRelativePath(
            string path)
        {
            return WithProp(o =>
            {
                var name = path.SplitPath().Last();
                o.RelativePath.Returns(path);
                o.Name.Returns(name);
            });
        }
    }
}