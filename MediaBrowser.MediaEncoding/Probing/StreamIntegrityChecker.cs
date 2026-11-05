using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediaBrowser.MediaEncoding.Probing
{
    /// <summary>
    /// Spots files whose audio or video stream ends well before the rest of the file, which is what a cut-off download
    /// or a bad remux looks like. Players stall or go silent at the point where the short stream ends.
    /// </summary>
    internal static class StreamIntegrityChecker
    {
        private const double MinGapSeconds = 10;
        private const double MinGapFraction = 0.01;

        /// <summary>
        /// Describes every audio or video stream that is clearly shorter than the longest one.
        /// </summary>
        /// <param name="data">The ffprobe result.</param>
        /// <returns>One message per short stream, empty if the file looks fine or the streams carry no durations.</returns>
        public static IReadOnlyList<string> FindProblems(InternalMediaInfoResult data)
        {
            var streams = (data.Streams ?? Array.Empty<MediaStreamInfo>())
                .Where(s => (s.CodecType == CodecType.Audio || s.CodecType == CodecType.Video)
                    && s.Disposition?.GetValueOrDefault("attached_pic") != 1)
                .Select(s => (Stream: s, Seconds: GetSeconds(s)))
                .Where(s => s.Seconds.HasValue)
                .ToList();

            if (streams.Count == 0)
            {
                return Array.Empty<string>();
            }

            // The container's own length counts too, so a lone stream that falls short of it is still caught.
            var longest = streams.Max(s => s.Seconds!.Value);
            if (double.TryParse(data.Format?.Duration, NumberStyles.Float, CultureInfo.InvariantCulture, out var formatSeconds))
            {
                longest = Math.Max(longest, formatSeconds);
            }

            var allowedGap = Math.Max(MinGapSeconds, longest * MinGapFraction);

            return streams
                .Where(s => longest - s.Seconds!.Value > allowedGap)
                .Select(s => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} stream #{1} ({2}) ends at {3} but the file runs to {4}",
                    s.Stream.CodecType == CodecType.Audio ? "Audio" : "Video",
                    s.Stream.Index,
                    s.Stream.CodecName,
                    TimeSpan.FromSeconds(s.Seconds!.Value).ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture),
                    TimeSpan.FromSeconds(longest).ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture)))
                .ToList();
        }

        private static double? GetSeconds(MediaStreamInfo stream)
        {
            if (double.TryParse(stream.Duration, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
            {
                return seconds;
            }

            // Matroska often has no stream duration, only a tag.
            return ProbeResultNormalizer.GetRuntimeSecondsFromTags(stream);
        }
    }
}
