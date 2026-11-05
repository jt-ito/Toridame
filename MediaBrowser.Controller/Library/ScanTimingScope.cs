using System;
using System.Diagnostics;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Records the time between its creation and disposal against a <see cref="ScanStage"/>.
/// </summary>
public readonly struct ScanTimingScope : IDisposable, IEquatable<ScanTimingScope>
{
    private readonly ScanStage _stage;
    private readonly long _start;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScanTimingScope"/> struct.
    /// </summary>
    /// <param name="stage">The stage being timed.</param>
    internal ScanTimingScope(ScanStage stage)
    {
        _stage = stage;
        _start = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Compares two scopes for equality.
    /// </summary>
    /// <param name="left">The first scope.</param>
    /// <param name="right">The second scope.</param>
    /// <returns><c>true</c> if they are equal.</returns>
    public static bool operator ==(ScanTimingScope left, ScanTimingScope right) => left.Equals(right);

    /// <summary>
    /// Compares two scopes for inequality.
    /// </summary>
    /// <param name="left">The first scope.</param>
    /// <param name="right">The second scope.</param>
    /// <returns><c>true</c> if they differ.</returns>
    public static bool operator !=(ScanTimingScope left, ScanTimingScope right) => !left.Equals(right);

    /// <inheritdoc />
    public void Dispose() => ScanTimings.Add(_stage, Stopwatch.GetTimestamp() - _start);

    /// <inheritdoc />
    public bool Equals(ScanTimingScope other) => _stage == other._stage && _start == other._start;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ScanTimingScope other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_stage, _start);
}
