using System;
using System.Collections.Generic;
using MediaBrowser.MediaEncoding.Probing;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Probing
{
    public class StreamIntegrityCheckerTests
    {
        private static MediaStreamInfo Stream(int index, CodecType type, string? duration, string? tagDuration = null)
        {
            return new MediaStreamInfo
            {
                Index = index,
                CodecType = type,
                CodecName = type == CodecType.Audio ? "aac" : "h264",
                Duration = duration,
                Tags = tagDuration is null ? null : new Dictionary<string, string?> { ["DURATION"] = tagDuration }
            };
        }

        private static InternalMediaInfoResult Result(string? formatDuration, params MediaStreamInfo[] streams)
        {
            return new InternalMediaInfoResult
            {
                Streams = streams,
                Format = new MediaFormatInfo { Duration = formatDuration }
            };
        }

        [Fact]
        public void FindProblems_AudioEndsMinutesEarly_ReportsTheAudioStream()
        {
            var problems = StreamIntegrityChecker.FindProblems(Result(
                "1440",
                Stream(0, CodecType.Video, "1440"),
                Stream(1, CodecType.Audio, "1200")));

            var problem = Assert.Single(problems);
            Assert.Contains("Audio stream #1", problem, StringComparison.Ordinal);
            Assert.Contains("00:20:00", problem, StringComparison.Ordinal);
        }

        [Fact]
        public void FindProblems_MatroskaTagDurations_AreUsedWhenStreamDurationIsMissing()
        {
            var problems = StreamIntegrityChecker.FindProblems(Result(
                "1440",
                Stream(0, CodecType.Video, null, "00:24:00.000000000"),
                Stream(1, CodecType.Audio, null, "00:10:00.000000000")));

            Assert.Contains("Audio stream #1", Assert.Single(problems), StringComparison.Ordinal);
        }

        [Fact]
        public void FindProblems_SmallDifferences_AreIgnored()
        {
            var problems = StreamIntegrityChecker.FindProblems(Result(
                "1440",
                Stream(0, CodecType.Video, "1440"),
                Stream(1, CodecType.Audio, "1436")));

            Assert.Empty(problems);
        }

        [Fact]
        public void FindProblems_StreamShorterThanContainer_IsReportedEvenWhenAlone()
        {
            var problems = StreamIntegrityChecker.FindProblems(Result("1440", Stream(0, CodecType.Video, "600")));

            Assert.Contains("Video stream #0", Assert.Single(problems), StringComparison.Ordinal);
        }

        [Fact]
        public void FindProblems_NoDurationsAvailable_ReportsNothing()
        {
            var problems = StreamIntegrityChecker.FindProblems(Result(
                "1440",
                Stream(0, CodecType.Video, null),
                Stream(1, CodecType.Audio, null)));

            Assert.Empty(problems);
        }
    }
}
