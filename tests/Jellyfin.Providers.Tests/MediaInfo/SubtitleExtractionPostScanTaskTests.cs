using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class SubtitleExtractionPostScanTaskTests
{
    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task Run_QueuesTheExtractionTaskOnlyWhenEnabled(bool enabled, int expectedQueues)
    {
        var taskManager = new Mock<ITaskManager>();
        var config = new Mock<IServerConfigurationManager>();
        config.SetupGet(c => c.Configuration).Returns(new ServerConfiguration { ExtractSubtitlesAfterLibraryScan = enabled });
        var task = new SubtitleExtractionPostScanTask(taskManager.Object, config.Object);

        await task.Run(new Progress<double>(), CancellationToken.None);

        taskManager.Verify(t => t.QueueIfNotRunning<SubtitleExtractionTask>(), Times.Exactly(expectedQueues));
        taskManager.VerifyNoOtherCalls();
    }
}
