using System.Diagnostics;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core;

/// <summary>
/// 简单的监控一个函数的执行时间.用于调试的时候快速的测试一个函数的执行时间
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