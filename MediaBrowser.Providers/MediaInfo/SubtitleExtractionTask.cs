using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo;

/// <summary>
/// Extracts embedded text subtitles ahead of time, so the first playback doesn't wait on a full-file ffmpeg pass.
/// </summary>
/// <remarks>
/// Safe to stop and rerun: videos whose cached subtitles are still fresh are skipped without starting ffmpeg.
/// </remarks>
public class SubtitleExtractionTask : IScheduledTask
{
    private const int QueryPageLimit = 100;

    private readonly ILogger<SubtitleExtractionTask> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly ISubtitleEncoder _subtitleEncoder;
    private readonly ITranscodeManager _transcodeManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleExtractionTask"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="localization">The localization manager.</param>
    /// <param name="mediaSourceManager">The media source manager.</param>
    /// <param name="subtitleEncoder">The subtitle encoder.</param>
    /// <param name="transcodeManager">The transcode manager.</param>
    public SubtitleExtractionTask(
        ILogger<SubtitleExtractionTask> logger,
        ILibraryManager libraryManager,
        ILocalizationManager localization,
        IMediaSourceManager mediaSourceManager,
        ISubtitleEncoder subtitleEncoder,
        ITranscodeManager transcodeManager)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _localization = localization;
        _mediaSourceManager = mediaSourceManager;
        _subtitleEncoder = subtitleEncoder;
        _transcodeManager = transcodeManager;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskExtractSubtitles");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskExtractSubtitlesDescription");

    /// <inheritdoc />
    public string Key => "ExtractSubtitles";

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            // Startup resumes an interrupted first run; the daily run picks up newly added videos.
            new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger },
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromHours(3.5).Ticks
            }
        ];
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var query = new InternalItemsQuery
        {
            MediaTypes = [MediaType.Video],
            SourceTypes = [SourceType.Library],
            IsVirtualItem = false,
            IsFolder = false,
            Recursive = true,
            IncludeOwnedItems = true,
            Limit = QueryPageLimit
        };

        var numberOfVideos = _libraryManager.GetCount(query);

        var startIndex = 0;
        var numComplete = 0;

        while (startIndex < numberOfVideos)
        {
            query.StartIndex = startIndex;
            var videos = _libraryManager.GetItemList(query).OfType<Video>();

            foreach (var video in videos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ExtractAsync(video, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error extracting subtitles for {ItemName}", video.Name);
                }

                numComplete++;
                progress.Report(100d * numComplete / numberOfVideos);
            }

            startIndex += QueryPageLimit;
        }

        progress.Report(100);
    }

    private async Task ExtractAsync(Video video, CancellationToken cancellationToken)
    {
        var sources = _mediaSourceManager.GetStaticMediaSources(video, false)
            .Where(source => source.MediaStreams.Any(IsExtractable))
            .ToList();

        if (sources.Count == 0)
        {
            return;
        }

        // Stay out of the way of anyone watching; the extraction is a full-file read.
        await _transcodeManager.WaitForIdleAsync(cancellationToken).ConfigureAwait(false);

        foreach (var source in sources)
        {
            // Image subtitles (pgs, vobsub) are burned in by default, so they stay on-demand.
            await _subtitleEncoder.ExtractAllExtractableSubtitles(source, cancellationToken, lowPriority: true, textOnly: true).ConfigureAwait(false);
        }
    }

    private static bool IsExtractable(MediaStream stream)
        => stream is { IsExternal: false, IsTextSubtitleStream: true, SupportsExternalStream: true };
}
