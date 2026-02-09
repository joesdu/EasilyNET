using EasilyNET.Raft.Storage.File.Options;

namespace EasilyNET.Raft.Storage.File.Stores;

internal sealed class FlushPolicyDecider(RaftFileStorageOptions options)
{
    private DateTime _lastFlushUtc = DateTime.MinValue;
    private DateTime _windowStartUtc = DateTime.UtcNow;
    private int _windowWrites;

    public bool ShouldFlushNow()
    {
        var now = DateTime.UtcNow;
        _windowWrites++;
        // ReSharper disable once InvertIf
        if ((now - _windowStartUtc).TotalSeconds >= 1)
        {
            _windowStartUtc = now;
            _windowWrites = 1;
        }
        return options.FsyncPolicy switch
        {
            FsyncPolicy.Always   => true,
            FsyncPolicy.Batch    => CheckBatch(now),
            FsyncPolicy.Adaptive => _windowWrites <= Math.Max(1, options.AdaptiveHighLoadWritesPerSecond) || CheckBatch(now),
            _                    => true
        };
    }

    private bool CheckBatch(DateTime now)
    {
        if (_lastFlushUtc == DateTime.MinValue)
        {
            _lastFlushUtc = now;
            return true;
        }
        if ((now - _lastFlushUtc).TotalMilliseconds < Math.Max(1, options.BatchFlushIntervalMs))
        {
            return false;
        }
        _lastFlushUtc = now;
        return true;
    }
}