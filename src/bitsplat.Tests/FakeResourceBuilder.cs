using System.IO;
using bitsplat.Storage;
using NSubstitute;
using PeanutButter.RandomGenerators;
using PeanutButter.Utils;
using static bitsplat.Tests.RandomValueGen;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    public class FakeResourceBuilder : GenericBuilder<FakeResourceBuilder, IFileResource>
    {
        public override IFileResource ConstructEntity()
        {
            var result = Substitute.For<IFileResource>();
            result.SetMetadata("basePath", GetRandomPath());
            return result;
        }

        public override FakeResourceBuilder WithRandomProps()
        {
            return WithProp(RandomRelativePath)
                .WithProp(RandomSize)
                .WithProp(o =>
                {
                    var basePath = o.GetMetadata<string>("basePath");
                    var relPath = o.RelativePath;
                    o.Path.Returns(Path.Combine(basePath, relPath));
                });
        }

        private void RandomRelativePath(IFileResource obj)
        {
            obj.RelativePath.Returns(GetRandomPath());
        }

        private void RandomSize(IFileResource obj)
        {
            obj.Size.Returns(GetRandomInt(100, 1024));
        }
        
        public FakeResourceBuilder WithBasePath(string basePath)
        {
            return WithProp(o =>
            {
                o.Path.Returns(Path.Combine(basePath, o.RelativePath));
            });
        }
    }
}