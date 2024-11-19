using System.Diagnostics;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core;

/// <summary>
/// 性能监控
/// </summary>
public static class PerformanceMonitor
{
    /// <summary>
    /// </summary>
    /// <param name="action"></param>
    /// <param name="functionName"></param>
    /// <param name="logger"></param>
    public static void MeasureExecutionTime(Action action, string functionName, Action<string> logger)
    {
        var start = Stopwatch.GetTimestamp();
        action();
        var end = Stopwatch.GetTimestamp();
        logger.Invoke($"{functionName} executed in {Stopwatch.GetElapsedTime(start, end).TotalMilliseconds} ms");
    }
}