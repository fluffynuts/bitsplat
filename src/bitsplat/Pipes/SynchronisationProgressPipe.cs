using System;
using System.Collections.Generic;
using System.Linq;
using bitsplat.Storage;

namespace bitsplat.Pipes
{
    public class SynchronisationProgressPipe
        : PassThrough,
          IPassThrough, // explicitly define as IPassThrough to clarify
          ISyncQueueNotifiable
    {
        private readonly IProgressReporter _reporter;
        private int _current;
        private int _totalBatchResources;
        private IFileResource _currentSource;
        private IFileResource _currentTarget;
        private long _totalWritten = 0;
        private long _currentWritten = 0;
        private string _batchLabel;

        public SynchronisationProgressPipe(
            IProgressReporter reporter
        )
        {
            _reporter = reporter;
        }

        protected override void OnWrite(
            byte[] buffer,
            int count)
        {
            if (_currentSource == null)
            {
                return;
            }

            _totalWritten += count;
            _currentWritten += count;

            var percentageComplete =
                BoundValue(
                    1,
                    99,
                    (int) Math.Floor((100M * _currentWritten) / _currentSource.Size)
                );
            _reporter.NotifyCurrent(
                _currentSource?.RelativePath,
                percentageComplete
            );
        }

        private int BoundValue(
            int min,
            int max,
            int current)
        {
            if (current > max)
            {
                return max;
            }

            return current < min
                       ? min
                       : current;
        }

        protected override void OnEnd()
        {
        }

        public void NotifySyncBatchStart(
            string label,
            IEnumerable<IFileResource> sourceResources
        )
        {
            var (total, longestName) = sourceResources.Aggregate(
                (total: 0, longest: 0),
                (acc, cur) => (acc.total + 1,
                               acc.longest > cur.RelativePath.Length
                                   ? acc.longest
                                   : cur.RelativePath.Length)
            );
            if (total == 0)
            {
                return; // nothing to do
            }

            _batchLabel = label;
            _totalBatchResources = total;
            _reporter.SetMaxLabelLength(longestName);
            _reporter.NotifyOverall(
                label,
                _current,
                _totalBatchResources
            );
        }

        public void NotifySyncBatchComplete(
            string label,
            IEnumerable<IFileResource> sourceResources
        )
        {
            var total = sourceResources.Count();
            if (total == 0)
            {
                return; // nothing to do
            }

            _reporter.NotifyOverall(
                label,
                total,
                total
            );
            ClearBatch();
        }

        public void NotifySyncStart(
            IFileResource sourceResource,
            IFileResource targetResource
        )
        {
            Clear();
            _reporter.NotifyOverall(_batchLabel, ++_current, _totalBatchResources);
            _currentSource = sourceResource;
            _currentTarget = targetResource;
            _reporter.NotifyCurrent(
                sourceResource.RelativePath,
                0
            );
        }

        public void NotifySyncComplete(
            IFileResource sourceResource,
            IFileResource targetResource)
        {
            _reporter.NotifyCurrent(
                sourceResource.RelativePath,
                100
            );
            Clear();
        }

        public void NotifyError(
            IFileResource sourceResource,
            IFileResource targetResource,
            Exception ex)
        {
            _reporter.NotifyError(
                sourceResource.RelativePath
            );
            // TODO: do something with the exception
            // -> this is likely to end up in logs, so it may be useful to log
            //     and allow continuation after error?
        }

        public void NotifySyncBatchPrepare(string label,
            IFileSystem source,
            IFileSystem target)
        {
            _reporter.NotifyPrepare(
                source,
                target
            );
        }

        public override void Dispose()
        {
            Clear();
            base.Dispose();
        }

        private void ClearBatch()
        {
            Clear();
            _totalWritten = 0;
            _batchLabel = null;
        }

        private void Clear()
        {
            _currentSource = null;
            _currentTarget = null;
            _currentWritten = 0;
        }
    }
}