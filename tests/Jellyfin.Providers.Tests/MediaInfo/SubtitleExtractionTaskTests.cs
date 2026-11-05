using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class SubtitleExtractionTaskTests
{
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<IMediaSourceManager> _mediaSourceManager = new();
    private readonly Mock<ISubtitleEncoder> _subtitleEncoder = new();
    private readonly Mock<ITranscodeManager> _transcodeManager = new();
    private readonly SubtitleExtractionTask _task;

    public SubtitleExtractionTaskTests()
    {
        var video = new Video { Name = "Test" };
        _libraryManager.Setup(l => l.GetCount(It.IsAny<InternalItemsQuery>())).Returns(1);
        _libraryManager.Setup(l => l.GetItemList(It.IsAny<InternalItemsQuery>())).Returns(new List<BaseItem> { video });

        _task = new SubtitleExtractionTask(
            NullLogger<SubtitleExtractionTask>.Instance,
            _libraryManager.Object,
            Mock.Of<ILocalizationManager>(),
            _mediaSourceManager.Object,
            _subtitleEncoder.Object,
            _transcodeManager.Object);
    }

    [Fact]
    public async Task ExecuteAsync_TextSubtitle_ExtractsTextOnlyAtLowPriorityAfterIdle()
    {
        SetSources(Stream("subrip"), Stream("pgssub"));

        await _task.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        _transcodeManager.Verify(t => t.WaitForIdleAsync(It.IsAny<CancellationToken>()), Times.Once);
        _subtitleEncoder.Verify(
            e => e.ExtractAllExtractableSubtitles(It.IsAny<MediaSourceInfo>(), It.IsAny<CancellationToken>(), true, true),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ImageSubtitlesOnly_DoesNothing()
    {
        SetSources(Stream("pgssub"));

        await _task.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        _transcodeManager.Verify(t => t.WaitForIdleAsync(It.IsAny<CancellationToken>()), Times.Never);
        _subtitleEncoder.Verify(
            e => e.ExtractAllExtractableSubtitles(It.IsAny<MediaSourceInfo>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ExtractionThrows_KeepsGoingAndCompletes()
    {
        SetSources(Stream("subrip"));
        _subtitleEncoder
            .Setup(e => e.ExtractAllExtractableSubtitles(It.IsAny<MediaSourceInfo>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await _task.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        _subtitleEncoder.Verify(
            e => e.ExtractAllExtractableSubtitles(It.IsAny<MediaSourceInfo>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Cancelled_Throws()
    {
        SetSources(Stream("subrip"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _task.ExecuteAsync(new Progress<double>(), cts.Token));
    }

    private static MediaStream Stream(string codec) => new()
    {
        Type = MediaStreamType.Subtitle,
        Codec = codec,
        IsExternal = false,
        SupportsExternalStream = true
    };

    private void SetSources(params MediaStream[] streams)
    {
        _mediaSourceManager
            .Setup(m => m.GetStaticMediaSources(It.IsAny<BaseItem>(), It.IsAny<bool>(), It.IsAny<User>()))
            .Returns(new List<MediaSourceInfo> { new() { MediaStreams = streams } });
    }
}
