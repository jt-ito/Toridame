using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Accumulates how long each <see cref="ScanStage"/> takes, so a slow library scan can be traced to a stage.
/// </summary>
/// <remarks>
/// Totals are summed over all parallel workers, so they can exceed the scan's wall-clock time.
/// Stages documented as nested are already included in the stage that contains them.
/// </remarks>
public static class ScanTimings
{
    private static readonly long[] _elapsedTimestamps = new long[Enum.GetValues<ScanStage>().Length];
    private static readonly long[] _counts = new long[Enum.GetValues<ScanStage>().Length];

    /// <summary>
    /// Starts timing a stage. Dispose the result to stop.
    /// </summary>
    /// <param name="stage">The stage being timed.</param>
    /// <returns>A scope that records the elapsed time when disposed.</returns>
    public static ScanTimingScope Measure(ScanStage stage) => new(stage);

    /// <summary>
    /// Forgets everything measured so far.
    /// </summary>
    public static void Reset()
    {
        Array.Clear(_elapsedTimestamps);
        Array.Clear(_counts);
    }

    /// <summary>
    /// Describes the time spent in each stage that ran, as "Stage=seconds/count".
    /// </summary>
    /// <returns>The summary, or an empty string if nothing was measured.</returns>
    public static string Summarize()
    {
        var sb = new StringBuilder();
        foreach (var stage in Enum.GetValues<ScanStage>())
        {
            var count = Interlocked.Read(ref _counts[(int)stage]);
            if (count == 0)
            {
                continue;
            }

            var seconds = (double)Interlocked.Read(ref _elapsedTimestamps[(int)stage]) / Stopwatch.Frequency;
            if (sb.Length > 0)
            {
                sb.Append(", ");
            }

            sb.Append(CultureInfo.InvariantCulture, $"{stage}={seconds:F1}s/{count}");
        }

        return sb.ToString();
    }

    internal static void Add(ScanStage stage, long elapsedTimestamps)
    {
        Interlocked.Add(ref _elapsedTimestamps[(int)stage], elapsedTimestamps);
        Interlocked.Increment(ref _counts[(int)stage]);
    }
}
