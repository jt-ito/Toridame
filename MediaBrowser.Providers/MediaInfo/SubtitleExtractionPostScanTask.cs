using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Providers.MediaInfo;

/// <summary>
/// Starts the Extract Subtitles task after every library scan, if the server is set to, so new videos don't wait for the nightly run.
/// </summary>
public class SubtitleExtractionPostScanTask : ILibraryPostScanTask
{
    private readonly ITaskManager _taskManager;
    private readonly IServerConfigurationManager _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleExtractionPostScanTask"/> class.
    /// </summary>
    /// <param name="taskManager">The task manager.</param>
    /// <param name="config">The server configuration manager.</param>
    public SubtitleExtractionPostScanTask(ITaskManager taskManager, IServerConfigurationManager config)
    {
        _taskManager = taskManager;
        _config = config;
    }

    /// <inheritdoc />
    public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        if (_config.Configuration.ExtractSubtitlesAfterLibraryScan)
        {
            // Only queues it. The extraction can take hours, and the scan must not stay open for that long.
            _taskManager.QueueIfNotRunning<SubtitleExtractionTask>();
        }

        progress.Report(100);

        return Task.CompletedTask;
    }
}
