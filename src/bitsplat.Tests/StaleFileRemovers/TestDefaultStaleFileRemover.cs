using System.IO;
using System.Linq;
using bitsplat.CommandLine;
using bitsplat.Pipes;
using bitsplat.StaleFileRemovers;
using bitsplat.Storage;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using NExpect;
using NSubstitute;
using NSubstitute.Extensions;
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

        [Test]
        public void ShouldKeepWatchedMarkersWhoseMediaFileIsStillAtSource()
        {
            // Arrange
            var media = ResourceAt("Series/ep1.mkv");
            var source = Substitute.For<IFileSystem>();
            var target = Substitute.For<IFileSystem>();
            source.ListResourcesRecursive()
                .Returns(new[] { media });
            var markerForMediaAtSource = ResourceAt("Series/ep1.mkv.t");
            var markerForMediaGoneFromSource = ResourceAt("Series/ep2.mkv.t");
            target.ListResourcesRecursive()
                .Returns(new[] { media, markerForMediaAtSource, markerForMediaGoneFromSource });
            var sut = Create();
            // Act
            sut.RemoveStaleFiles(
                source,
                target);
            // Assert
            Expect(target)
                .To.Have.Received(1)
                .Delete(Arg.Any<string>());
            Expect(target)
                .To.Have.Received(1)
                .Delete("Series/ep2.mkv.t");
        }

        private static IReadWriteFileResource ResourceAt(string relativePath)
        {
            return ReadWriteFileResourceBuilder.Create()
                .WithRandomProps()
                .WithRelativePath(relativePath)
                .Build();
        }

        [Test]
        public void ShouldOnlyReportWhatWouldBeRemovedOnDryRun()
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
            var options = Substitute.For<IOptions>();
            options.DryRun.Returns(true);
            var messageWriter = Substitute.For<IMessageWriter>();
            var sut = Create(options, messageWriter);
            // Act
            sut.RemoveStaleFiles(
                source,
                target);
            // Assert
            Expect(target)
                .Not.To.Have.Received()
                .Delete(Arg.Any<string>());
            Expect(messageWriter)
                .To.Have.Received(1)
                .Write($"Would remove target: {onlyAtTarget.RelativePath}");
        }

        private static IStaleFileRemover Create(
            IOptions options = null,
            IMessageWriter messageWriter = null)
        {
            return new DefaultStaleFileRemover(
                new FakeProgressReporter(),
                options ?? Substitute.For<IOptions>(),
                messageWriter ?? Substitute.For<IMessageWriter>()
            );
        }
    }

    public class ReadWriteFileResourceBuilder
        : GenericBuilder<ReadWriteFileResourceBuilder, IReadWriteFileResource>
    {
        public override IReadWriteFileResource ConstructEntity()
        {
            return Substitute.For<IReadWriteFileResource>();
        }

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
            string path
        )
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