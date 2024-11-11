// ***********************************************************************
// Copyright (c) luxy. All rights reserved.
// ***********************************************************************

using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;

namespace WinFormAutoDISample.Listeners;

/// <inheritdoc />
public sealed class PerformanceListener(ILogger<PerformanceListener> logger) : EventListener
{
    /// <summary>
    /// CPU使用率更新事件
    /// </summary>
    public event EventHandler<Tuple<double, string?>> CpuUsageUpdated;

    /// <summary>
    /// 内存使用率更新事件
    /// </summary>
    public event EventHandler<Tuple<double, string?>> MemoryUsageUpdated;

    /// <inheritdoc />
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        base.OnEventSourceCreated(eventSource);
        ArgumentNullException.ThrowIfNull(eventSource);
        if (eventSource.Name == "System.Runtime")
        {
            EnableEvents(eventSource, EventLevel.Critical, (EventKeywords)(-1), new Dictionary<string, string?> { ["EventCounterIntervalSec"] = "1" });
        }
    }

    /// <inheritdoc />
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.EventName is not "EventCounters" || eventData.Payload is null) return;
        foreach (var payload in eventData.Payload)
        {
            if (payload is null) continue;
            var metrics = (IDictionary<string, object>)payload;
            switch (metrics["Name"])
            {
                case "cpu-usage":
                    {
                        logger.LogInformation("CPU USAGE:{CPU}", Math.Round((double)metrics["Mean"], 2));
                        CpuUsageUpdated?.Invoke(this, new(Math.Round((double)metrics["Mean"], 2), metrics["DisplayUnits"].ToString()));
                        break;
                    }
                case "working-set":
                    {
                        MemoryUsageUpdated?.Invoke(this, new(Math.Round((double)metrics["Mean"], 2), metrics["DisplayUnits"].ToString()));
                        break;
                    }
            }
        }
    }
}