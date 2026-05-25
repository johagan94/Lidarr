using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download
{
    [TestFixture]
    public class DownloadEventHubFixture : CoreTest<DownloadEventHub>
    {
        private TrackedDownload _trackedDownload;

        [SetUp]
        public void Setup()
        {
            _trackedDownload = new TrackedDownload
            {
                DownloadClient = 1,
                DownloadItem = Builder<DownloadClientItem>.CreateNew()
                    .With(v => v.DownloadId = "download-id")
                    .With(v => v.Title = "Album")
                    .With(v => v.CanBeRemoved = true)
                    .With(v => v.DownloadClientInfo = new DownloadClientItemClientInfo { Name = "client" })
                    .Build()
            };

            Mocker.GetMock<IDownloadClient>()
                .SetupGet(v => v.Definition)
                .Returns(new DownloadClientDefinition
                {
                    Name = "client",
                    RemoveCompletedDownloads = true
                });

            Mocker.GetMock<IProvideDownloadClient>()
                .Setup(v => v.Get(1))
                .Returns(Mocker.GetMock<IDownloadClient>().Object);
        }

        [Test]
        public void should_stop_tracking_after_download_is_removed()
        {
            Subject.Handle(new DownloadCanBeRemovedEvent(_trackedDownload));

            Mocker.GetMock<ITrackedDownloadService>()
                .Verify(v => v.StopTracking(_trackedDownload.DownloadItem.DownloadId), Times.Once());
        }
    }
}
