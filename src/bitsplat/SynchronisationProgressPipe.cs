using System.Collections.Generic;
using System.Linq;
using bitsplat.Pipes;
using bitsplat.Storage;

namespace bitsplat
{
    public interface IProgressReporter
    {
        void NotifyCurrent(string label, int percentComplete);
        void NotifyOverall(int current, int total);
    }

    public class SynchronisationProgressPipe
        : PassThrough,
          ISyncQueueNotifiable
    {
        private readonly IProgressReporter _reporter;
        private int _current;
        private int _total;

        public SynchronisationProgressPipe(
            IProgressReporter reporter
        )
        {
            _reporter = reporter;
        }

        protected override void OnWrite(byte[] buffer, int count)
        {
        }

        protected override void OnEnd()
        {
        }

        public void NotifySyncBatchStart(
            IEnumerable<IFileResourceProperties> sourceResources
        )
        {
            _current = 0;
            _total = sourceResources.Count();
            _reporter.NotifyOverall(
                _current,
                _total
            );
        }

        public void NotifySyncBatchComplete(
            IEnumerable<IFileResourceProperties> sourceResources
        )
        {
            var total = sourceResources.Count();
            _reporter.NotifyOverall(
                total,
                total
            );
        }

        public void NotifySyncStart(
            IFileResourceProperties sourceResource,
            IFileResourceProperties targetResource
        )
        {
            _reporter.NotifyOverall(++_current, _total);
            _reporter.NotifyCurrent(
                sourceResource.RelativePath,
                0
            );
        }

        public void NotifySyncComplete(
            IFileResourceProperties sourceResource,
            IFileResourceProperties targetResource)
        {
            _reporter.NotifyCurrent(
                sourceResource.RelativePath,
                100
            );
        }
    }
}