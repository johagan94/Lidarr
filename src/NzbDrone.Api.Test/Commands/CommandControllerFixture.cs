using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lidarr.Api.V1.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Composition;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.SignalR;
using NzbDrone.Test.Common;

namespace NzbDrone.Api.Test.Commands
{
    [TestFixture]
    public class CommandControllerFixture : TestBase<CommandController>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant(new KnownTypes(new List<Type>
            {
                typeof(RefreshMonitoredDownloadsCommand)
            }));

            Mocker.GetMock<IManageCommandQueue>()
                .Setup(v => v.Push(It.IsAny<RefreshMonitoredDownloadsCommand>(),
                    It.IsAny<CommandPriority>(),
                    It.IsAny<CommandTrigger>()))
                .Returns(new CommandModel
                {
                    Id = 1,
                    Name = "RefreshMonitoredDownloads",
                    Body = new RefreshMonitoredDownloadsCommand()
                });

            Mocker.GetMock<IManageCommandQueue>()
                .Setup(v => v.Get(1))
                .Returns(new CommandModel
                {
                    Id = 1,
                    Name = "RefreshMonitoredDownloads",
                    Body = new RefreshMonitoredDownloadsCommand()
                });

            Mocker.GetMock<IBroadcastSignalRMessage>()
                .SetupGet(v => v.IsConnected)
                .Returns(false);
        }

        [Test]
        public void should_queue_refresh_monitored_downloads_at_high_priority()
        {
            var json = "{\"name\":\"RefreshMonitoredDownloads\"}";
            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            context.Request.ContentType = "application/json";

            Subject.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };

            Subject.StartCommand(new CommandResource { Name = "RefreshMonitoredDownloads" });

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(v => v.Push(It.IsAny<RefreshMonitoredDownloadsCommand>(),
                    CommandPriority.High,
                    CommandTrigger.Manual), Times.Once());
        }
    }
}
