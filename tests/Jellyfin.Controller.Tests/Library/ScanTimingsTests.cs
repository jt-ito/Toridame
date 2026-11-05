using System.Threading;
using MediaBrowser.Controller.Library;
using Xunit;

namespace Jellyfin.Controller.Tests.Library;

[Collection("ScanTimings")]
public class ScanTimingsTests
{
    [Fact]
    public void Summarize_NothingMeasured_IsEmpty()
    {
        ScanTimings.Reset();

        Assert.Equal(string.Empty, ScanTimings.Summarize());
    }

    [Fact]
    public void Measure_RecordsOnlyStagesThatRan()
    {
        ScanTimings.Reset();

        using (ScanTimings.Measure(ScanStage.Enumerate))
        {
            Thread.Sleep(5);
        }

        using (ScanTimings.Measure(ScanStage.Enumerate))
        {
        }

        var summary = ScanTimings.Summarize();
        Assert.Contains("Enumerate=", summary, System.StringComparison.Ordinal);
        Assert.EndsWith("/2", summary, System.StringComparison.Ordinal);
        Assert.DoesNotContain("Providers", summary, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Reset_ClearsEarlierMeasurements()
    {
        using (ScanTimings.Measure(ScanStage.Ffprobe))
        {
        }

        ScanTimings.Reset();

        Assert.Equal(string.Empty, ScanTimings.Summarize());
    }
}
