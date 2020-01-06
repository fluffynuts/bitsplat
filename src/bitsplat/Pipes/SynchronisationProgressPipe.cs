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
        private int _currentItem;
        private int _totalBatchItems;
        private IFileResource _currentSource;
        private IFileResource _currentTarget;
        private long _batchBytesTransferred = 0;
        private long _currentWritten = 0;
        private string _batchLabel;
        private long _totalBatchBytes;

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

            _batchBytesTransferred += count;
            _currentWritten += count;

            _reporter.NotifyCurrent(
                new NotificationDetails()
                {
                    Label = _currentSource.RelativePath,
                    CurrentBytesTransferred = _currentWritten,
                    CurrentTotalBytes = _currentSource.Size,
                    CurrentItem = _currentItem,
                    TotalItems = _totalBatchItems,
                    TotalBytesTransferred = _batchBytesTransferred,
                    TotalBytes = _totalBatchBytes
                    // TODO: current / total items?
                }
            );
        }

        protected override void OnEnd()
        {
        }

        public void NotifySyncBatchStart(
            string label,
            IEnumerable<IFileResource> sourceResources
        )
        {
            var (totalItems, longestName, totalBytes) = sourceResources.Aggregate(
                (totalItems: 0, longestName: 0, totalBytes: 0L),
                (acc, cur) => (acc.totalItems + 1,
                               acc.longestName > cur.RelativePath.Length
                                   ? acc.longestName
                                   : cur.RelativePath.Length,
                               acc.totalBytes + cur.Size
                              )
            );
            if (totalItems == 0)
            {
                return; // nothing to do
            }

            _batchLabel = label;
            _totalBatchItems = totalItems;
            _totalBatchBytes = totalBytes;
            _reporter.SetMaxLabelLength(longestName);
            _reporter.NotifyOverall(
                new NotificationDetails()
                {
                    Label = label,
                    CurrentItem = _currentItem,
                    TotalItems = _totalBatchItems,
                    CurrentBytesTransferred = 0,
                    CurrentTotalBytes = totalBytes,
                    TotalBytesTransferred = _batchBytesTransferred,
                    TotalBytes = _totalBatchBytes
                });
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
                new NotificationDetails()
                {
                    Label = label,
                    TotalItems = total,
                    CurrentItem = total,
                    TotalBytesTransferred = _batchBytesTransferred,
                    TotalBytes = _totalBatchBytes
                });
            ClearBatch();
        }

        public void NotifySyncStart(
            IFileResource sourceResource,
            IFileResource targetResource
        )
        {
            Clear();
            _reporter.NotifyOverall(
                new NotificationDetails()
                {
                    Label = _batchLabel,
                    CurrentItem = ++_currentItem,
                    TotalItems = _totalBatchItems,
                    TotalBytesTransferred = _batchBytesTransferred,
                    TotalBytes = _totalBatchBytes
                }
            );
            _currentSource = sourceResource;
            _currentTarget = targetResource;
            _reporter.NotifyCurrent(
                new NotificationDetails()
                {
                    Label = sourceResource.RelativePath,
                    CurrentItem = _currentItem,
                    TotalItems = _totalBatchItems,
                    CurrentBytesTransferred = 0,
                    CurrentTotalBytes = sourceResource.Size,
                    TotalBytesTransferred = _batchBytesTransferred,
                    TotalBytes = _totalBatchBytes
                }
            );
        }

        public void NotifySyncComplete(
            IFileResource sourceResource,
            IFileResource targetResource)
        {
            _reporter.NotifyCurrent(
                new NotificationDetails()
                {
                    Label = sourceResource.RelativePath,
                    CurrentItem = _currentItem,
                    TotalItems = _totalBatchItems,
                    CurrentBytesTransferred = sourceResource.Size,
                    CurrentTotalBytes = sourceResource.Size,
                    TotalBytesTransferred = _batchBytesTransferred,
                    TotalBytes = _totalBatchBytes
                }
            );
            Clear();
        }

        public void NotifyError(
            IFileResource sourceResource,
            IFileResource targetResource,
            Exception ex)
        {
            _reporter.NotifyError(
                new NotificationDetails()
                {
                    Label = sourceResource.RelativePath,
                    CurrentBytesTransferred = _currentWritten,
                    CurrentTotalBytes = sourceResource.Size,
                    CurrentItem = _currentItem,
                    TotalItems = _totalBatchItems,
                    TotalBytesTransferred = _batchBytesTransferred,
                    TotalBytes = _totalBatchBytes,
                    Exception = ex
                }
            );
        }

        public void NotifySyncBatchPrepare(string label,
            IFileSystem source,
            IFileSystem target)
        {
            _reporter.NotifyPrepare(
                label,
                source,
                target
            );
        }

        public void NotifyNoWork(
            IFileSystem source,
            IFileSystem target)
        {
            _reporter.NotifyNoWork(
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
            _batchBytesTransferred = 0;
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